using System.Text;

namespace Lighthouse;

internal static class Utilities
{
    internal static string BLEHexToMAC(string hexString)
    {
        if (string.IsNullOrEmpty(hexString) || hexString.Length <= 2)
        {
            return hexString;
        }

        var result = new StringBuilder();

        for (var i = 0; i < hexString.Length; i++)
        {
            if (i > 0 && i % 2 == 0)
                result.Append(':');

            result.Append(hexString[i]);
        }

        return result.ToString();
    }
}