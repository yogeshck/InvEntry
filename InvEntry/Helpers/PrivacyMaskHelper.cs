namespace InvEntry.Helpers;

public static class PrivacyMaskHelper
{
    public static string Mask(
        object? value,
        int visibleLastCharacters = 4)
    {
        if (value is null)
            return string.Empty;


        var text =
            value.ToString()?.Trim();


        if (string.IsNullOrEmpty(text))
            return string.Empty;


        if (visibleLastCharacters < 0)
            visibleLastCharacters = 0;


        if (text.Length <= visibleLastCharacters)
            return text;


        var maskedLength =
            text.Length -
            visibleLastCharacters;


        return
            new string('*', maskedLength) +
            text[^visibleLastCharacters..];
    }
}