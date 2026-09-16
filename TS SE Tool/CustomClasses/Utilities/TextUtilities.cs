/*
   Copyright 2016-2022 LIPtoH <liptoh.codebase@gmail.com>
   Licensed under the Apache License, Version 2.0 (the "License");
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TS_SE_Tool.Utilities
{
    public class TextUtilities
    {
        public static string FromHexToString(string input)
        {
            try
            {
                if (string.IsNullOrEmpty(input) || input.Length % 2 != 0) return null;
                byte[] raw = new byte[input.Length / 2];
                for (int i = 0; i < raw.Length; i++) raw[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);
                return new UTF8Encoding(false, true).GetString(raw);
            }
            catch { return null; }
        }

        public static string FromUtfHexToString(string input)
        {
            if (input == null) return null;
            try
            {
                StringBuilder result = new StringBuilder();
                int index = 0;
                while (index < input.Length)
                {
                    if (index + 3 < input.Length && input[index] == '\\' && input[index + 1] == 'x')
                    {
                        List<byte> bytes = new List<byte>();
                        while (index + 3 < input.Length && input[index] == '\\' && input[index + 1] == 'x')
                        {
                            bytes.Add(Convert.ToByte(input.Substring(index + 2, 2), 16));
                            index += 4;
                        }
                        result.Append(new UTF8Encoding(false, true).GetString(bytes.ToArray()));
                    }
                    else
                    {
                        result.Append(input[index]);
                        index++;
                    }
                }
                return result.ToString();
            }
            catch { return input; }
        }

        public static string FromStringToHex(string input)
        {
            try { return ByteArrayToString(Encoding.UTF8.GetBytes(input ?? string.Empty)); }
            catch { return null; }
        }

        public static string FromStringToOutputString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "\"\"";
            string encoded = StringToByteArrayStringFull(input);
            return CheckStringAlphaNumeric(encoded) ? input : "\"" + encoded + "\"";
        }

        public static string FromOutputStringToString(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string value = input;
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                value = value.Substring(1, value.Length - 2);
            return FromUtfHexToString(value);
        }

        public static string ByteArrayToString(byte[] bytes) { return BitConverter.ToString(bytes).Replace("-", ""); }

        public static string StringToByteArrayStringFull(string input)
        {
            StringBuilder output = new StringBuilder();
            foreach (char character in input ?? string.Empty)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(new[] { character });
                if (bytes.Length == 1 && (char.IsLetterOrDigit(character) || character == '_')) output.Append(character);
                else foreach (byte value in bytes) output.Append("\\x").Append(value.ToString("x2"));
            }
            return output.ToString();
        }

        internal static string CapitalizeWord(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        public static bool CheckStringAlphaNumeric(string input)
        {
            foreach (char character in input) if (!char.IsLetterOrDigit(character) && character != '_') return false;
            return true;
        }

        internal static string CheckAndClearStringFromQuotes(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (input.Length >= 2 && input.StartsWith("\"") && input.EndsWith("\""))
                return FromUtfHexToString(input.Substring(1, input.Length - 2));
            return input;
        }

        private static readonly Regex regexDigit = new Regex(@"\d+", RegexOptions.Compiled);
        internal static bool ExtractFirstNumber(string text, out int result)
        {
            result = -1;
            Match match = regexDigit.Match(text ?? string.Empty);
            if (match.Success) result = Convert.ToInt32(match.Value);
            return match.Success;
        }
    }
}
