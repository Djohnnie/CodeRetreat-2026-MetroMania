using System.IO.Compression;
using System.Text;

namespace MetroMania.Application.Helpers;

public static class SvgCompression
{
    public static byte[] Compress(string svgContent)
    {
        var bytes = Encoding.UTF8.GetBytes(svgContent);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            gzip.Write(bytes);
        return output.ToArray();
    }

    public static string Decompress(byte[] svgzBytes)
    {
        using var input = new MemoryStream(svgzBytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
