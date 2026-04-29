using System.IO.Compression;
using System.Text;
using ZstdSharp;

namespace LogSystem.Core.Services.Common.Compression;

/// <summary>
/// Factory for creating compression strategy instances.
/// </summary>
public class CompressionFactory
{
    private readonly ICompressionStrategy _brotliStrategy;
    private readonly ICompressionStrategy _gzipStrategy;
    private readonly ICompressionStrategy _zstdStrategy;
    private readonly ICompressionStrategy _deflateStrategy;

    public CompressionFactory()
    {
        // Create internal instances with configured compression levels
        _brotliStrategy = new BrotliCompressionStrategy(CompressionLevel.Optimal);
        _gzipStrategy = new GzipCompressionStrategy(CompressionLevel.Optimal);
        _zstdStrategy = new ZstdCompressionStrategy(compressionLevel: 6); // Recommended mid-level
        _deflateStrategy = new DeflateCompressionStrategy(CompressionLevel.Optimal);
    }

    /// <summary>
    /// Gets the default compression strategy (Brotli with Optimal level).
    /// </summary>
    public ICompressionStrategy GetDefaultStrategy()
    {
        return _brotliStrategy;
    }

    /// <summary>
    /// Gets a compression strategy based on the file name extension.
    /// </summary>
    /// <param name="fileName">File name with compression extension (e.g., "file.json.br", "file.json.gzip")</param>
    /// <returns>Compression strategy matching the file extension</returns>
    /// <exception cref="ArgumentException">Thrown when fileName is null/empty or has unknown compression extension</exception>
    public ICompressionStrategy GetStrategyFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        if (_brotliStrategy.HasFormatExtension(fileName))
            return _brotliStrategy;

        if (_gzipStrategy.HasFormatExtension(fileName))
            return _gzipStrategy;

        if (_zstdStrategy.HasFormatExtension(fileName))
            return _zstdStrategy;

        if (_deflateStrategy.HasFormatExtension(fileName))
            return _deflateStrategy;

        throw new ArgumentException($"Unknown compression format in file name: {fileName}. Supported extensions: .br, .gzip, .gz, .zst, .zstd, .deflate", nameof(fileName));
    }

    /// <summary>
    /// Gets the Brotli compression strategy.
    /// </summary>
    public ICompressionStrategy GetBrotliStrategy()
    {
        return _brotliStrategy;
    }

    /// <summary>
    /// Gets the Gzip compression strategy.
    /// </summary>
    public ICompressionStrategy GetGzipStrategy()
    {
        return _gzipStrategy;
    }

    /// <summary>
    /// Gets the Zstandard compression strategy.
    /// </summary>
    public ICompressionStrategy GetZstdStrategy()
    {
        return _zstdStrategy;
    }

    /// <summary>
    /// Gets the Deflate compression strategy.
    /// </summary>
    public ICompressionStrategy GetDeflateStrategy()
    {
        return _deflateStrategy;
    }

    /*
    /// <summary>
    /// Compresses a string using Brotli algorithm.
    /// </summary>
    /// <param name="content">String content to compress. Cannot be null.</param>
    /// <param name="compressionLevel">Compression level (Optimal, Fastest, SmallestSize, or NoCompression). Must be specified.</param>
    /// <returns>Compressed byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    /// <exception cref="ArgumentException">Thrown when compressionLevel is not a valid CompressionLevel value.</exception>
    public byte[] CompressBrotli(string content, CompressionLevel compressionLevel)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return CompressBrotli(bytes, compressionLevel);
    }

    /// <summary>
    /// Compresses a byte array using Brotli algorithm.
    /// </summary>
    /// <param name="content">Byte array to compress. Cannot be null.</param>
    /// <param name="compressionLevel">Compression level (Optimal, Fastest, SmallestSize, or NoCompression). Must be specified.</param>
    /// <returns>Compressed byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    /// <exception cref="ArgumentException">Thrown when compressionLevel is not a valid CompressionLevel value.</exception>
    public byte[] CompressBrotli(byte[] content, CompressionLevel compressionLevel)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (!Enum.IsDefined(typeof(CompressionLevel), compressionLevel))
            throw new ArgumentException(
                "Invalid compression level. Use CompressionLevel.Optimal, Fastest, SmallestSize, or NoCompression.",
                nameof(compressionLevel));

        using var outputStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(outputStream, compressionLevel))
        {
            brotliStream.Write(content, 0, content.Length);
        }
        return outputStream.ToArray();
    }

    /// <summary>
    /// Compresses a string using Gzip algorithm.
    /// </summary>
    /// <param name="content">String content to compress. Cannot be null.</param>
    /// <param name="compressionLevel">Compression level (Optimal, Fastest, SmallestSize, or NoCompression). Must be specified.</param>
    /// <returns>Compressed byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    /// <exception cref="ArgumentException">Thrown when compressionLevel is not a valid CompressionLevel value.</exception>
    public byte[] CompressGzip(string content, CompressionLevel compressionLevel)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return CompressGzip(bytes, compressionLevel);
    }

    /// <summary>
    /// Compresses a byte array using Gzip algorithm.
    /// </summary>
    /// <param name="content">Byte array to compress. Cannot be null.</param>
    /// <param name="compressionLevel">Compression level (Optimal, Fastest, SmallestSize, or NoCompression). Must be specified.</param>
    /// <returns>Compressed byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    /// <exception cref="ArgumentException">Thrown when compressionLevel is not a valid CompressionLevel value.</exception>
    public byte[] CompressGzip(byte[] content, CompressionLevel compressionLevel)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (!Enum.IsDefined(typeof(CompressionLevel), compressionLevel))
            throw new ArgumentException(
                "Invalid compression level. Use CompressionLevel.Optimal, Fastest, SmallestSize, or NoCompression.",
                nameof(compressionLevel));

        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, compressionLevel))
        {
            gzipStream.Write(content, 0, content.Length);
        }
        return outputStream.ToArray();
    }

    /// <summary>
    /// Compresses a string using Zstandard algorithm.
    /// </summary>
    /// <param name="content">String content to compress. Cannot be null.</param>
    /// <param name="compressionLevel">Compression level from 1 (fastest) to 22 (maximum). Recommended: 3-9. Must be specified.</param>
    /// <returns>Compressed byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when compressionLevel is not between 1 and 22.</exception>
    public byte[] CompressZstd(string content, int compressionLevel)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return CompressZstd(bytes, compressionLevel);
    }

    /// <summary>
    /// Compresses a byte array using Zstandard algorithm.
    /// </summary>
    /// <param name="content">Byte array to compress. Cannot be null.</param>
    /// <param name="compressionLevel">Compression level from 1 (fastest) to 22 (maximum). Recommended: 3-9. Must be specified.</param>
    /// <returns>Compressed byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when compressionLevel is not between 1 and 22.</exception>
    public byte[] CompressZstd(byte[] content, int compressionLevel)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (compressionLevel < 1 || compressionLevel > 22)
            throw new ArgumentOutOfRangeException(nameof(compressionLevel),
                "Zstandard compression level must be between 1 (fastest) and 22 (maximum). Recommended: 3-9.");

        using var compressor = new Compressor(compressionLevel);
        return compressor.Wrap(content).ToArray();
    }

    /// <summary>
    /// Compresses a string using Deflate algorithm.
    /// </summary>
    /// <param name="content">String content to compress. Cannot be null.</param>
    /// <param name="compressionLevel">Compression level (Optimal, Fastest, SmallestSize, or NoCompression). Must be specified.</param>
    /// <returns>Compressed byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    /// <exception cref="ArgumentException">Thrown when compressionLevel is not a valid CompressionLevel value.</exception>
    public byte[] CompressDeflate(string content, CompressionLevel compressionLevel)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        byte[] bytes = Encoding.UTF8.GetBytes(content);
        return CompressDeflate(bytes, compressionLevel);
    }

    /// <summary>
    /// Compresses a byte array using Deflate algorithm.
    /// </summary>
    /// <param name="content">Byte array to compress. Cannot be null.</param>
    /// <param name="compressionLevel">Compression level (Optimal, Fastest, SmallestSize, or NoCompression). Must be specified.</param>
    /// <returns>Compressed byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    /// <exception cref="ArgumentException">Thrown when compressionLevel is not a valid CompressionLevel value.</exception>
    public byte[] CompressDeflate(byte[] content, CompressionLevel compressionLevel)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (!Enum.IsDefined(typeof(CompressionLevel), compressionLevel))
            throw new ArgumentException(
                "Invalid compression level. Use CompressionLevel.Optimal, Fastest, SmallestSize, or NoCompression.",
                nameof(compressionLevel));

        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(outputStream, compressionLevel))
        {
            deflateStream.Write(content, 0, content.Length);
        }
        return outputStream.ToArray();
    }

    /// <summary>
    /// Decompresses data that was compressed using Brotli algorithm.
    /// </summary>
    /// <param name="compressedContent">Compressed byte array. Cannot be null.</param>
    /// <returns>Decompressed string decoded as UTF8.</returns>
    /// <exception cref="ArgumentNullException">Thrown when compressedContent is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when data cannot be decompressed or is not valid Brotli data.</exception>
    /// <exception cref="DecoderFallbackException">Thrown when decompressed data is not valid UTF8.</exception>
    public string DecompressBrotli(byte[] compressedContent)
    {
        if (compressedContent == null)
            throw new ArgumentNullException(nameof(compressedContent));

        using var inputStream = new MemoryStream(compressedContent);
        using var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        brotliStream.CopyTo(outputStream);
        byte[] decompressedBytes = outputStream.ToArray();

        return Encoding.UTF8.GetString(decompressedBytes);
    }

    /// <summary>
    /// Decompresses data that was compressed using Gzip algorithm.
    /// </summary>
    /// <param name="compressedContent">Compressed byte array. Cannot be null.</param>
    /// <returns>Decompressed string decoded as UTF8.</returns>
    /// <exception cref="ArgumentNullException">Thrown when compressedContent is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when data cannot be decompressed or is not valid Gzip data.</exception>
    /// <exception cref="DecoderFallbackException">Thrown when decompressed data is not valid UTF8.</exception>
    public string DecompressGzip(byte[] compressedContent)
    {
        if (compressedContent == null)
            throw new ArgumentNullException(nameof(compressedContent));

        using var inputStream = new MemoryStream(compressedContent);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        gzipStream.CopyTo(outputStream);
        byte[] decompressedBytes = outputStream.ToArray();

        return Encoding.UTF8.GetString(decompressedBytes);
    }

    /// <summary>
    /// Decompresses data that was compressed using Zstandard algorithm.
    /// </summary>
    /// <param name="compressedContent">Compressed byte array. Cannot be null.</param>
    /// <returns>Decompressed string decoded as UTF8.</returns>
    /// <exception cref="ArgumentNullException">Thrown when compressedContent is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when data cannot be decompressed or is not valid Zstandard data.</exception>
    /// <exception cref="DecoderFallbackException">Thrown when decompressed data is not valid UTF8.</exception>
    public string DecompressZstd(byte[] compressedContent)
    {
        if (compressedContent == null)
            throw new ArgumentNullException(nameof(compressedContent));

        using var decompressor = new Decompressor();
        byte[] decompressedBytes = decompressor.Unwrap(compressedContent).ToArray();

        return Encoding.UTF8.GetString(decompressedBytes);
    }

    /// <summary>
    /// Decompresses data that was compressed using Deflate algorithm.
    /// </summary>
    /// <param name="compressedContent">Compressed byte array. Cannot be null.</param>
    /// <returns>Decompressed string decoded as UTF8.</returns>
    /// <exception cref="ArgumentNullException">Thrown when compressedContent is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when data cannot be decompressed or is not valid Deflate data.</exception>
    /// <exception cref="DecoderFallbackException">Thrown when decompressed data is not valid UTF8.</exception>
    public string DecompressDeflate(byte[] compressedContent)
    {
        if (compressedContent == null)
            throw new ArgumentNullException(nameof(compressedContent));

        using var inputStream = new MemoryStream(compressedContent);
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        deflateStream.CopyTo(outputStream);
        byte[] decompressedBytes = outputStream.ToArray();

        return Encoding.UTF8.GetString(decompressedBytes);
    }
    */
}
