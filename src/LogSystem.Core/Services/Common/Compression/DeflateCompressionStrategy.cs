using System.IO.Compression;
using System.Text;

namespace LogSystem.Core.Services.Common.Compression;

public class DeflateCompressionStrategy : ICompressionStrategy
{
    private readonly CompressionLevel _compressionLevel;

    public DeflateCompressionStrategy(CompressionLevel compressionLevel)
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
        using (var deflateStream = new DeflateStream(outputStream, _compressionLevel))
        {
            deflateStream.Write(content, 0, content.Length);
        }
        return outputStream.ToArray();
    }

    public string Decompress(byte[] content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        using var inputStream = new MemoryStream(content);
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        deflateStream.CopyTo(outputStream);
        byte[] decompressedBytes = outputStream.ToArray();

        return Encoding.UTF8.GetString(decompressedBytes);
    }

    public string AddFormatExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        return fileName.EndsWith(".deflate", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.deflate";
    }

    public bool HasFormatExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        return fileName.EndsWith(".deflate", StringComparison.OrdinalIgnoreCase);
    }

    public string GetMimeContentType()
    {
        return "application/deflate";
    }

    public string GetContentEncoding()
    {
        return "deflate";
    }
}
