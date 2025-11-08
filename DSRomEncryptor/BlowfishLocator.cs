using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DSRomEncryptor;

sealed class BlowfishLocator
{
    private const string NTR_BLOWFISH_NAME = "ntrBlowfish.bin";
    private const string TWL_BLOWFISH_NAME = "twlBlowfish.bin";
    private const string TWL_DEV_BLOWFISH_NAME = "twlDevBlowfish.bin";
    private const string BIOS_NDS_7_NAME = "biosnds7.rom";
    private const string BIOS_DSI_7_NAME = "biosdsi7.rom";
    private const int BIOS_NDS_7_LENGTH = 0x4000;
    private const int BIOS_NDS_7_BLOWFISH_OFFSET = 0x30;
    private const int BIOS_DSI_7_LENGTH = 0x10000;
    private const int BIOS_DSI_7_BLOWFISH_OFFSET = 0xC6D0;

    public bool TryGetNtrBlowfish([NotNullWhen(returnValue: true)] out byte[]? ntrBlowfish)
    {
        bool success = false;
        ntrBlowfish = null;

        // Try to get directly from a file
        if (TryLoadFileFromApplicationFolder(NTR_BLOWFISH_NAME, out var blowfish) &&
            blowfish.Length == Blowfish.KEY_TABLE_LENGTH)
        {
            ntrBlowfish = blowfish;
            success = true;
        }

        // Try to get from a DS arm7 bios dump
        if (!success && TryLoadFileFromApplicationFolder(BIOS_NDS_7_NAME, out var biosNds7) &&
            biosNds7.Length == BIOS_NDS_7_LENGTH)
        {
            ntrBlowfish = biosNds7[BIOS_NDS_7_BLOWFISH_OFFSET..(BIOS_NDS_7_BLOWFISH_OFFSET + Blowfish.KEY_TABLE_LENGTH)];
            success = true;
        }

        return success;
    }

    public bool TryGetTwlBlowfish([NotNullWhen(returnValue: true)] out byte[]? twlBlowfish)
    {
        bool success = false;
        twlBlowfish = null;

        // Try to get directly from a file
        if (TryLoadFileFromApplicationFolder(TWL_BLOWFISH_NAME, out var blowfish)
            && blowfish.Length == Blowfish.KEY_TABLE_LENGTH)
        {
            twlBlowfish = blowfish;
            success = true;
        }

        // Try to get from a DSi arm7 bios dump
        if (!success && TryLoadFileFromApplicationFolder(BIOS_DSI_7_NAME, out var biosDsi7) &&
            biosDsi7.Length == BIOS_DSI_7_LENGTH)
        {
            twlBlowfish = biosDsi7[BIOS_DSI_7_BLOWFISH_OFFSET..(BIOS_DSI_7_BLOWFISH_OFFSET + Blowfish.KEY_TABLE_LENGTH)];
            success = true;
        }

        return success;
    }

    public bool TryGetTwlDevBlowfish([NotNullWhen(returnValue: true)] out byte[]? twlDevBlowfish)
    {
        // Try to get directly from a file
        return TryLoadFileFromApplicationFolder(TWL_DEV_BLOWFISH_NAME, out twlDevBlowfish);
    }

    private bool TryLoadFileFromApplicationFolder(string fileName, [NotNullWhen(returnValue: true)] out byte[]? fileData)
    {
        string appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        try
        {
            fileData = File.ReadAllBytes(Path.Combine(appDirectory, fileName));
            return true;
        }
        catch
        {
            fileData = null;
            return false;
        }
    }
}
