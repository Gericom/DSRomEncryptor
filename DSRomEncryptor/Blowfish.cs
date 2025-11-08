namespace DSRomEncryptor;

/// <summary>
/// Class for performing Blowfish encryption and decryption.
/// </summary>
sealed class Blowfish
{
    public const int P_TABLE_ENTRY_COUNT = 18;
    public const int S_BOX_COUNT = 4;
    public const int S_BOX_ENTRY_COUNT = 256;
    public const int KEY_TABLE_P_TABLE_LENGTH = P_TABLE_ENTRY_COUNT * sizeof(uint);
    public const int KEY_TABLE_S_BOXES_LENGTH = S_BOX_COUNT * S_BOX_ENTRY_COUNT * sizeof(uint);
    public const int KEY_TABLE_LENGTH = KEY_TABLE_P_TABLE_LENGTH + KEY_TABLE_S_BOXES_LENGTH;
    public const int BLOCK_LENGTH = 8;

    private const string DATA_LENGTH_NOT_MULTIPLE_OF_8_EXCEPTION_MESSAGE = "Data length must be a multiple of 8.";
    private const string DESTINATION_BUFFER_TOO_SMALL_EXCEPTION_MESSAGE = "Insufficient space in destination buffer.";

    private readonly uint[] _pTable;
    private readonly uint[][] _sBoxes;

    public Blowfish(ReadOnlySpan<byte> keyTable)
    {
        if (keyTable.Length < KEY_TABLE_LENGTH)
        {
            throw new ArgumentException("Key table length is invalid.", nameof(keyTable));
        }

        _pTable = keyTable.ReadU32Le(0, P_TABLE_ENTRY_COUNT);
        _sBoxes = new uint[S_BOX_COUNT][];
        _sBoxes[0] = keyTable.ReadU32Le(0x48, S_BOX_ENTRY_COUNT);
        _sBoxes[1] = keyTable.ReadU32Le(0x448, S_BOX_ENTRY_COUNT);
        _sBoxes[2] = keyTable.ReadU32Le(0x848, S_BOX_ENTRY_COUNT);
        _sBoxes[3] = keyTable.ReadU32Le(0xC48, S_BOX_ENTRY_COUNT);
    }

    /// <summary>
    /// Encrypts the data in the given <paramref name="data"/> span in place.
    /// The length of the span must be a multiple of 8.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    public void Encrypt(Span<byte> data)
    {
        Encrypt(data, data);
    }

    /// <summary>
    /// Encrypts the data in the given <paramref name="src"/> span and writes it to the <paramref name="dst"/> span.
    /// The length of the <paramref name="src"/> span must be a multiple of 8.
    /// <paramref name="dst"/> must have a length equal to or larger than <paramref name="src"/>.
    /// </summary>
    /// <param name="src">The data to encrypt.</param>
    /// <param name="dst">The span to write the encrypted data to.</param>
    public void Encrypt(ReadOnlySpan<byte> src, Span<byte> dst)
    {
        ThrowIfDataLengthNotMultipleOf8(src.Length, nameof(src));
        if (dst.Length < src.Length)
        {
            throw new ArgumentException(DESTINATION_BUFFER_TOO_SMALL_EXCEPTION_MESSAGE, nameof(dst));
        }

        for (int i = 0; i < src.Length; i += BLOCK_LENGTH)
        {
            ulong value = Encrypt(src.ReadU64Le(i));
            dst.WriteU64Le(i, value);
        }
    }

    /// <summary>
    /// Encrypts a single 64 bit value.
    /// </summary>
    /// <param name="val">The value to encrypt.</param>
    /// <returns>The encrypted value.</returns>
    public ulong Encrypt(ulong val)
    {
        uint y = (uint)(val & 0xFFFFFFFF);
        uint x = (uint)(val >> 32);
        for (int i = 0; i < 16; i++)
        {
            uint z = _pTable[i] ^ x;
            uint a = _sBoxes[0][z >> 24 & 0xFF];
            uint b = _sBoxes[1][z >> 16 & 0xFF];
            uint c = _sBoxes[2][z >> 8 & 0xFF];
            uint d = _sBoxes[3][z & 0xFF];
            x = d + (c ^ b + a) ^ y;
            y = z;
        }

        return x ^ _pTable[16] | (ulong)(y ^ _pTable[17]) << 32;
    }

    /// <summary>
    /// Decrypts the data in the given <paramref name="data"/> span in place.
    /// The length of the span must be a multiple of 8.
    /// </summary>
    /// <param name="data">The data to decrypt.</param>
    public void Decrypt(Span<byte> data)
    {
        Decrypt(data, data);
    }

    /// <summary>
    /// Decrypts the data in the given <paramref name="src"/> span and writes it to the <paramref name="dst"/> span.
    /// The length of the <paramref name="src"/> span must be a multiple of 8.
    /// <paramref name="dst"/> must have a length equal to or larger than <paramref name="src"/>.
    /// </summary>
    /// <param name="src">The data to decrypt.</param>
    /// <param name="dst">The span to write the decrypted data to.</param>
    public void Decrypt(ReadOnlySpan<byte> src, Span<byte> dst)
    {
        ThrowIfDataLengthNotMultipleOf8(src.Length, nameof(src));
        if (dst.Length < src.Length)
        {
            throw new ArgumentException(DESTINATION_BUFFER_TOO_SMALL_EXCEPTION_MESSAGE, nameof(dst));
        }

        for (int i = 0; i < src.Length; i += BLOCK_LENGTH)
        {
            ulong val = Decrypt(src.ReadU64Le(i));
            dst.WriteU64Le(i, val);
        }
    }

    /// <summary>
    /// Decrypts a single 64 bit value.
    /// </summary>
    /// <param name="val">The value to decrypt.</param>
    /// <returns>The decrypted value.</returns>
    public ulong Decrypt(ulong val)
    {
        uint y = (uint)(val & 0xFFFFFFFF);
        uint x = (uint)(val >> 32);
        for (int i = 17; i >= 2; i--)
        {
            uint z = _pTable[i] ^ x;
            uint a = _sBoxes[0][z >> 24 & 0xFF];
            uint b = _sBoxes[1][z >> 16 & 0xFF];
            uint c = _sBoxes[2][z >> 8 & 0xFF];
            uint d = _sBoxes[3][z & 0xFF];
            x = d + (c ^ b + a) ^ y;
            y = z;
        }

        return x ^ _pTable[1] | (ulong)(y ^ _pTable[0]) << 32;
    }


    private void ThrowIfDataLengthNotMultipleOf8(int dataLength, string paramName)
    {
        if ((dataLength & 7) != 0)
        {
            throw new ArgumentException(DATA_LENGTH_NOT_MULTIPLE_OF_8_EXCEPTION_MESSAGE, paramName);
        }
    }
}