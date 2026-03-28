using System.Text;

namespace MetroMania.Domain.Extensions;

public static class StringExtensions
{
    extension(string value)
    {
        public string Base64Encode() =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

        public string Base64Decode() =>
            Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }
}
