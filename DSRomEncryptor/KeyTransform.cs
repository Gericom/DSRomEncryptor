namespace DSRomEncryptor;

/// <summary>
/// Class for transforming blowfish tables.
/// </summary>
static class KeyTransform
{
    /// <summary>
    /// Transforms the given <paramref name="keyTable"/> based on the given
    /// <paramref name="gameCode"/>, <paramref name="level"/> and <paramref name="modulo"/>.
    /// </summary>
    /// <param name="gameCode">The game code to use.</param>
    /// <param name="level">The transform level.</param>
    /// <param name="modulo">The modulo to use.</param>
    /// <param name="keyTable">The blowfish table to transform.</param>
    /// <returns>The transformed table.</returns>
    public static byte[] TransformTable(uint gameCode, int level, int modulo, ReadOnlySpan<byte> keyTable)
    {
        var newTable = keyTable[..Blowfish.KEY_TABLE_LENGTH].ToArray();

        Span<byte> keyCode = new byte[12];
        keyCode.WriteU32Le(0, gameCode);
        keyCode.WriteU32Le(4, gameCode >> 1);
        keyCode.WriteU32Le(8, gameCode << 1);
        if (level >= 1)
        {
            ApplyKeyCode(keyCode, modulo, newTable);
        }
        if (level >= 2)
        {
            ApplyKeyCode(keyCode, modulo, newTable);
        }

        keyCode.WriteU32Le(4, keyCode.ReadU32Le(4) << 1);
        keyCode.WriteU32Le(8, keyCode.ReadU32Le(8) >> 1);

        if (level >= 3)
        {
            ApplyKeyCode(keyCode, modulo, newTable);
        }

        return newTable;
    }

    private static void ApplyKeyCode(Span<byte> keyCode, int modulo, Span<byte> keyTable)
    {
        var blowfish = new Blowfish(keyTable);
        blowfish.Encrypt(keyCode[4..12]);
        blowfish.Encrypt(keyCode[0..8]);
        for (int i = 0; i < Blowfish.P_TABLE_ENTRY_COUNT; i++)
        {
            keyTable.WriteU32Le(i * 4, keyTable.ReadU32Le(i * 4) ^ keyCode.ReadU32Be(i * 4 % modulo));
        }

        var scratch = new byte[8];
        for (int i = 0; i < Blowfish.KEY_TABLE_LENGTH; i += Blowfish.BLOCK_LENGTH)
        {
            //update table
            blowfish = new Blowfish(keyTable);
            blowfish.Encrypt(scratch);
            scratch.AsSpan(4, 4).CopyTo(keyTable[i..]);
            scratch.AsSpan(0, 4).CopyTo(keyTable[(i + 4)..]);
        }
    }
}