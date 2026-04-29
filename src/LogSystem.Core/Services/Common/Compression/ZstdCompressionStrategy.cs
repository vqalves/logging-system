using System.Text;
using ZstdSharp;

namespace LogSystem.Core.Services.Common.Compression;

public class ZstdCompressionStrategy : ICompressionStrategy
{
    private readonly int _compressionLevel;

    public ZstdCompressionStrategy(int compressionLevel)
    {
        if (compressionLevel < 1 || compressionLevel > 22)
            throw new ArgumentOutOfRangeException(nameof(compressionLevel),
                "Zstandard compression level must be between 1 (fastest) and 22 (maximum). Recommended: 3-9.");

        _compressionLevel = compressionLevel;
    }

    public byte[] Compress(string content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return Compress(bytes);
    }

    public byte[] Compress(byte[] content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        using var compressor = new Compressor(_compressionLevel);
        return compressor.Wrap(content).ToArray();
    }

    public string Decompress(byte[] content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        using var decompressor = new Decompressor();
        byte[] decompressedBytes = decompressor.Unwrap(content).ToArray();

        return Encoding.UTF8.GetString(decompressedBytes);
    }

    public string AddFormatExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        return fileName.EndsWith(".zst", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".zstd", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.zst";
    }

    public bool HasFormatExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return fileName.EndsWith(".zst", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".zstd", StringComparison.OrdinalIgnoreCase);
    }

    public string GetMimeContentType()
    {
        return "application/zstd";
    }

    public string GetContentEncoding()
    {
        return "zstd";
    }
}
