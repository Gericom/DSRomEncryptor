using CommandLine;
using CommandLine.Text;

namespace DSRomEncryptor;

static class Program
{
    private const string HEADING_TEXT = "== DSRomEncryptor by Gericom ==";
    private const ulong SECURE_AREA_ID = 0x6A624F7972636E65UL; // "encryObj"
    private const int ROM_HEADER_GAME_CODE_OFFSET = 0xC;
    private const int ROM_HEADER_UNIT_CODE_OFFSET = 0x12;
    private const int ROM_HEADER_TWL_FLAGS_OFFSET = 0x1C;
    private const int ROM_HEADER_ARM9_OFFSET_OFFSET = 0x20;
    private const int ROM_HEADER_SECURE_AREA_CRC_OFFSET = 0x6C;
    private const int ROM_HEADER_NTR_AREA_END_OFFSET = 0x90;
    private const int ROM_HEADER_TWL_AREA_START_OFFSET = 0x92;
    private const int ROM_HEADER_CRC_OFFSET = 0x15E;
    private const int ROM_NTR_BLOWFISH_P_TABLE_OFFSET = 0x1600;
    private const int ROM_NTR_BLOWFISH_S_BOXES_OFFSET = 0x1C00;
    private const int ROM_SECURE_AREA_START_OFFSET = 0x4000;
    private const int ROM_ENCRYPTED_SECURE_AREA_END_OFFSET = 0x4800;
    private const int ROM_SECURE_AREA_END_OFFSET = 0x8000;
    private const int ROM_TWL_BLOWFISH_P_TABLE_OFFSET = 0x600;
    private const int ROM_TWL_BLOWFISH_S_BOXES_OFFSET = 0xC00;
    private const int TWL_CHUNK_SHIFT = 19;
    private const int TWL_CHUNK_SIZE = 0x80000;
    private const byte UNIT_CODE_TWL_FLAG = 2;
    private const byte TWL_FLAGS_HAS_TWL_EXCLUSIVE_AREA_FLAG = 1;

    private static void Main(string[] args)
    {
        var parser = new Parser(with => with.HelpWriter = null);
        var parserResult = parser.ParseArguments<CommandLineOptions>(args);
        parserResult
            .WithParsed(RunOptions)
            .WithNotParsed(_ =>
            {
                Console.WriteLine(HelpText.AutoBuild(parserResult, h =>
                {
                    h.AutoVersion = false;
                    h.Copyright = string.Empty;
                    h.Heading = HEADING_TEXT;
                    return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                }, e => e));
            });
    }

    private static void RunOptions(CommandLineOptions options)
    {
        var blowfishLocator = new BlowfishLocator();
        if (!blowfishLocator.TryGetNtrBlowfish(out var ntrBlowfish))
        {
            Console.WriteLine("Error: Could not load ntr blowfish key.");
            return;
        }

        var romData = File.ReadAllBytes(options.InputPath);
        var romDataSpan = romData.AsSpan();
        uint gameCode = romDataSpan.ReadU32Le(ROM_HEADER_GAME_CODE_OFFSET);
        var ntrTable = KeyTransform.TransformTable(gameCode, 2, 8, ntrBlowfish);
        ntrTable.AsSpan(0, Blowfish.KEY_TABLE_P_TABLE_LENGTH)
            .CopyTo(romDataSpan[ROM_NTR_BLOWFISH_P_TABLE_OFFSET..]);
        ntrTable.AsSpan(Blowfish.KEY_TABLE_P_TABLE_LENGTH, Blowfish.KEY_TABLE_S_BOXES_LENGTH)
            .CopyTo(romDataSpan[ROM_NTR_BLOWFISH_S_BOXES_OFFSET..]);

        InsertTestPatterns(romDataSpan);

        uint arm9Offset = romDataSpan.ReadU32Le(ROM_HEADER_ARM9_OFFSET_OFFSET);
        if (ROM_SECURE_AREA_END_OFFSET - arm9Offset > 0)
        {
            ushort secureCrc = Crc16.CalculateCrc16(romDataSpan[(int)arm9Offset..ROM_SECURE_AREA_END_OFFSET]);
            if (romDataSpan.ReadU16Le(ROM_HEADER_SECURE_AREA_CRC_OFFSET) != secureCrc)
            {
                var secureTable = KeyTransform.TransformTable(gameCode, 3, 8, ntrBlowfish);
                var blowfish = new Blowfish(secureTable);

                // secure area id
                ulong encryptedSecureAreaId = blowfish.Encrypt(SECURE_AREA_ID);
                encryptedSecureAreaId = new Blowfish(ntrTable).Encrypt(encryptedSecureAreaId);
                romDataSpan.WriteU64Le(ROM_SECURE_AREA_START_OFFSET, encryptedSecureAreaId);

                // encrypt secure area
                blowfish.Encrypt(romDataSpan[(ROM_SECURE_AREA_START_OFFSET + 8)..ROM_ENCRYPTED_SECURE_AREA_END_OFFSET]);

                // update secure area crc
                secureCrc = Crc16.CalculateCrc16(romDataSpan[(int)arm9Offset..ROM_SECURE_AREA_END_OFFSET]);
                romDataSpan.WriteU16Le(ROM_HEADER_SECURE_AREA_CRC_OFFSET, secureCrc);
            }
        }

        // twl
        try
        {
            if (ShouldRomHaveTwlArea(romDataSpan))
            {
                uint twlAreaStart = (uint)(romDataSpan.ReadU16Le(ROM_HEADER_TWL_AREA_START_OFFSET) * TWL_CHUNK_SIZE);
                if (twlAreaStart == 0)
                {
                    // Add twl area to rom
                    ushort twlAreaStartValue = (ushort)((romDataSpan.Length + (TWL_CHUNK_SIZE - 1)) >> TWL_CHUNK_SHIFT);
                    twlAreaStart = twlAreaStartValue * (uint)TWL_CHUNK_SIZE;
                    romDataSpan.WriteU16Le(ROM_HEADER_NTR_AREA_END_OFFSET, twlAreaStartValue);
                    romDataSpan.WriteU16Le(ROM_HEADER_TWL_AREA_START_OFFSET, twlAreaStartValue);
                    Array.Resize(ref romData, (int)(twlAreaStart + 0x8000));
                    romDataSpan = romData;
                }

                ReadOnlySpan<byte> twlTable;
                if (options.UseDsiDevBlowfish)
                {
                    if (!blowfishLocator.TryGetTwlDevBlowfish(out var twlDevBlowfish))
                    {
                        Console.WriteLine("Error: Could not load twl dev blowfish key.");
                        return;
                    }
                    twlTable = twlDevBlowfish;
                }
                else
                {
                    if (!blowfishLocator.TryGetTwlBlowfish(out var twlBlowfish))
                    {
                        Console.WriteLine("Error: Could not load twl blowfish key.");
                        return;
                    }
                    twlTable = KeyTransform.TransformTable(gameCode, 1, 8, twlBlowfish);
                }

                twlTable.Slice(0, Blowfish.KEY_TABLE_P_TABLE_LENGTH)
                    .CopyTo(romDataSpan[((int)twlAreaStart + ROM_TWL_BLOWFISH_P_TABLE_OFFSET)..]);
                twlTable.Slice(Blowfish.KEY_TABLE_P_TABLE_LENGTH, Blowfish.KEY_TABLE_S_BOXES_LENGTH)
                    .CopyTo(romDataSpan[((int)twlAreaStart + ROM_TWL_BLOWFISH_S_BOXES_OFFSET)..]);
            }
        }
        catch
        {
            Console.WriteLine("Warning: Couldn't insert twl blowfish.");
        }

        // Fix header CRC
        romDataSpan.WriteU16Le(ROM_HEADER_CRC_OFFSET, Crc16.CalculateCrc16(romDataSpan[..ROM_HEADER_CRC_OFFSET]));

        File.WriteAllBytes(options.OutputPath, romData);
    }

    private static bool ShouldRomHaveTwlArea(ReadOnlySpan<byte> romData)
    {
        return (romData[ROM_HEADER_UNIT_CODE_OFFSET] & UNIT_CODE_TWL_FLAG) != 0
            && (romData[ROM_HEADER_TWL_FLAGS_OFFSET] & TWL_FLAGS_HAS_TWL_EXCLUSIVE_AREA_FLAG) != 0;
    }

    private static void InsertTestPatterns(Span<byte> romData)
    {
        new byte[] { 0xFF, 0x0, 0xFF, 0x00, 0xAA, 0x55, 0xAA, 0x55 }.CopyTo(romData[0x3000..]);

        for (int i = 8; i < 0x200; i++)
        {
            romData[0x3000 + i] = (byte)(i & 0xFF);
        }

        for (int i = 0; i < 0x200; i++)
        {
            romData[0x3200 + i] = (byte)(0xFF - (i & 0xFF));
        }

        romData.Slice(0x3400, 0x200).Fill(0x00);
        romData.Slice(0x3600, 0x200).Fill(0xFF);
        romData.Slice(0x3800, 0x200).Fill(0x0F);
        romData.Slice(0x3A00, 0x200).Fill(0xF0);
        romData.Slice(0x3C00, 0x200).Fill(0x55);
        romData.Slice(0x3E00, 0x1FF).Fill(0xAA);

        romData[0x3FFF] = 0;
    }
}