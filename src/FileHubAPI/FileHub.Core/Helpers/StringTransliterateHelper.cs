using System.Text;

namespace FileHub.Core.Helpers;

public static class StringTransliterateHelper
{
    public static string Transliterate(string input)
    {
        string[] cyrillic =
        {
            "а", "б", "в", "г", "д", "е", "ё", "ж", "з", "и", "й", "к", "л", "м",
            "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ъ",
            "ы", "ь", "э", "ю", "я",
            "А", "Б", "В", "Г", "Д", "Е", "Ё", "Ж", "З", "И", "Й", "К", "Л", "М",
            "Н", "О", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ъ",
            "Ы", "Ь", "Э", "Ю", "Я"
        };

        string[] latin =
        {
            "a", "b", "v", "g", "d", "e", "e", "zh", "z", "i", "y", "k", "l", "m",
            "n", "o", "p", "r", "s", "t", "u", "f", "kh", "ts", "ch", "sh", "shch", "ie",
            "y", "", "e", "yu", "ya",
            "A", "B", "V", "G", "D", "E", "E", "Zh", "Z", "I", "Y", "K", "L", "M",
            "N", "O", "P", "R", "S", "T", "U", "F", "Kh", "Ts", "Ch", "Sh", "Shch", "Ie",
            "Y", "", "E", "Yu", "Ya"
        };

        var result = new StringBuilder();

        foreach (var c in input)
        {
            var index = Array.IndexOf(cyrillic, c.ToString());
            if (index != -1)
                result.Append(latin[index]);
            else
                result.Append(c);
        }

        return result.ToString();
    }
}