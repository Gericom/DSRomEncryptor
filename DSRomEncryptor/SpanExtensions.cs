using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DSRomEncryptor;

/// <summary>
/// Extensions for <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/>.
/// </summary>
static class SpanExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadU16Le(this Span<byte> span, int offset)
        => BinaryPrimitives.ReadUInt16LittleEndian(span[offset..]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadU16Le(this ReadOnlySpan<byte> span, int offset)
        => BinaryPrimitives.ReadUInt16LittleEndian(span[offset..]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteU16Le(this Span<byte> span, int offset, ushort value)
        => BinaryPrimitives.WriteUInt16LittleEndian(span[offset..], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadU32Le(this Span<byte> span, int offset)
        => BinaryPrimitives.ReadUInt32LittleEndian(span[offset..]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadU32Le(this ReadOnlySpan<byte> span, int offset)
        => BinaryPrimitives.ReadUInt32LittleEndian(span[offset..]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint[] ReadU32Le(this Span<byte> data, int offset, int count)
        => ReadU32Le((ReadOnlySpan<byte>)data, offset, count);

    public static uint[] ReadU32Le(this ReadOnlySpan<byte> data, int offset, int count)
    {
        var res = MemoryMarshal.Cast<byte, uint>(data[offset..(offset + count * 4)]).ToArray();
        if (!BitConverter.IsLittleEndian)
        {
            for (int i = 0; i < count; i++)
            {
                res[i] = BinaryPrimitives.ReverseEndianness(res[i]);
            }
        }

        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteU32Le(this Span<byte> span, int offset, uint value)
        => BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadU32Be(this Span<byte> span, int offset)
        => BinaryPrimitives.ReadUInt32BigEndian(span[offset..]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadU32Be(this ReadOnlySpan<byte> span, int offset)
        => BinaryPrimitives.ReadUInt32BigEndian(span[offset..]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteU32Be(this Span<byte> span, int offset, uint value)
        => BinaryPrimitives.WriteUInt32BigEndian(span[offset..], value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadU64Le(this Span<byte> span, int offset)
        => BinaryPrimitives.ReadUInt64LittleEndian(span[offset..]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadU64Le(this ReadOnlySpan<byte> span, int offset)
        => BinaryPrimitives.ReadUInt64LittleEndian(span[offset..]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteU64Le(this Span<byte> span, int offset, ulong value)
        => BinaryPrimitives.WriteUInt64LittleEndian(span[offset..], value);
}
