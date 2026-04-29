using System.IO.Compression;
using System.Text;
using ZstdSharp;

namespace LogSystem.Core.Services.Common.Compression;

public interface ICompressionStrategy
{
    string Decompress(byte[] content);
    byte[] Compress(string content);
    byte[] Compress(byte[] content);
    string AddFormatExtension(string fileName);
    bool HasFormatExtension(string fileName);

    // Used on AzureServices to upload file
    string GetMimeContentType();

    // Used on AzureServices to upload file
    string GetContentEncoding();
}
