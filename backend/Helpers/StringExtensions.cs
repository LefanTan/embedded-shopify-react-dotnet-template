using System.Text;

public static class StringExtensions
{
    public static string Base64Decode(this string base64EncodedData)
    {
        // Add padding characters if the string's length is not a multiple of 4
        int padding = base64EncodedData.Length % 4;
        if (padding > 0)
        {
            base64EncodedData += new string('=', 4 - padding);
        }

        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }

    public static string BinaryToBase64(this string binary)
    {
        byte[] bytes = Encoding.GetEncoding(28591).GetBytes(binary);
        string toReturn = System.Convert.ToBase64String(bytes);
        return toReturn;
    }
}
