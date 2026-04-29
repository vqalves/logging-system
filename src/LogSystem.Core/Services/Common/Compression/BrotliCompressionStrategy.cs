using System.IO.Compression;
using System.Text;

namespace LogSystem.Core.Services.Common.Compression;

public class BrotliCompressionStrategy : ICompressionStrategy
{
    private readonly CompressionLevel _compressionLevel;

    public BrotliCompressionStrategy(CompressionLevel compressionLevel)
    {
        if (!Enum.IsDefined(typeof(CompressionLevel), compressionLevel))
            throw new ArgumentException(
                "Invalid compression level. Use CompressionLevel.Optimal, Fastest, SmallestSize, or NoCompression.",
                nameof(compressionLevel));

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

        using var outputStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(outputStream, _compressionLevel))
        {
            brotliStream.Write(content, 0, content.Length);
        }
        return outputStream.ToArray();
    }

    public string Decompress(byte[] content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        using var inputStream = new MemoryStream(content);
        using var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        brotliStream.CopyTo(outputStream);
        byte[] decompressedBytes = outputStream.ToArray();

        return Encoding.UTF8.GetString(decompressedBytes);
    }

    public string AddFormatExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        return fileName.EndsWith(".br", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.br";
    }

    public bool HasFormatExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return fileName.EndsWith(".br", StringComparison.OrdinalIgnoreCase);
    }

    public string GetMimeContentType()
    {
        return "application/x-brotli";
    }

    public string GetContentEncoding()
    {
        return "br";
    }
}
