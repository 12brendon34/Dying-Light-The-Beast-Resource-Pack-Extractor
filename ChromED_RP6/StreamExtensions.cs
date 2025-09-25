using System.Buffers.Binary;

namespace ChromED_RP6
{
    public enum Endianness
    {
        LittleEndian,
        BigEndian
    }

    public static class StreamExtensions
    {
        public static Endianness DefaultEndianness { get; set; } = Endianness.LittleEndian;
        public static uint ReadUInt32(this Stream stream) => ReadUInt32(stream, DefaultEndianness);
        public static uint ReadUInt32(this Stream stream, Endianness endianness)
        {
            ArgumentNullException.ThrowIfNull(stream);

            // Span-based implementation (no allocations)
            Span<byte> buf = stackalloc byte[4];
            ReadExactlySpan(stream, buf);
            return endianness == Endianness.LittleEndian
                ? BinaryPrimitives.ReadUInt32LittleEndian(buf)
                : BinaryPrimitives.ReadUInt32BigEndian(buf);
        }
        private static void ReadExactlySpan(Stream stream, Span<byte> buffer)
        {
            int read = 0;
            while (read < buffer.Length)
            {
                // buffer[read..] is fine for Span<byte> and does not allocate
                int r = stream.Read(buffer[read..]);
                if (r == 0) throw new EndOfStreamException($"Unable to read {buffer.Length} bytes (got {read}).");
                read += r;
            }
        }
    }
}