namespace ScriptBoy.DiggableTerrains2D
{
    static class StringExtensions
    {
        public static string AddWordSpaces(this string input)
        {
            string output = string.Empty;
            bool isDigitPrev = false;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                bool isDigit = char.IsDigit(c);
                if (i > 0 && (isDigit || char.IsUpper(c) && !isDigitPrev))
                {
                    output += " ";
                }
                isDigitPrev = isDigit;
                output += c;
            }

            return output;
        }
    }
}