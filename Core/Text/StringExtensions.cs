﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Text;
using System.Text.Json;

namespace Trivial.Text
{
    /// <summary>
    /// Letter cases.
    /// </summary>
    public enum Cases
    {
        /// <summary>
        /// Keep original.
        /// </summary>
        Original = 0,

        /// <summary>
        /// Uppercase.
        /// </summary>
        Upper = 1,

        /// <summary>
        /// Lowercase.
        /// </summary>
        Lower = 2,

        /// <summary>
        /// First letter uppercase and rest keeping original.
        /// </summary>
        FirstLetterUpper = 3,

        /// <summary>
        /// First letter lowercase and rest keeping original.
        /// </summary>
        FirstLetterLower = 4
    }

    /// <summary>
    /// The string extension and helper.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Gets the MIME value of plain text format text.
        /// </summary>
        public const string PlainTextMIME = "text/plain";

        /// <summary>
        /// Returns a copy of this string converted to specific case, using the casing rules of the specified culture.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="options">The specific case.</param>
        /// <param name="culture">An object that supplies culture-specific casing rules.</param>
        /// <returns>The specific case equivalent of the current string.</returns>
        /// <exception cref="ArgumentNullException">culture is null.</exception>
        public static string ToSpecificCase(this string source, Cases options, CultureInfo culture = null)
        {
            if (string.IsNullOrWhiteSpace(source)) return source;
            switch (options)
            {
                case Cases.Original:
                    return source;
                case Cases.Upper:
                    return ToUpper(source, culture);
                case Cases.Lower:
                    return ToLower(source, culture);
                case Cases.FirstLetterUpper:
                    {
                        var s = source.TrimStart();
                        return $"{source.Substring(0, source.Length - s.Length)}{ToUpper(s.Substring(0, 1), culture)}{s.Substring(1)}";
                    }
                case Cases.FirstLetterLower:
                    {
                        var s = source.TrimStart();
                        return $"{source.Substring(0, source.Length - s.Length)}{ToLower(s.Substring(0, 1), culture)}{s.Substring(1)}";
                    }
                default:
                    return source;
            }
        }

        /// <summary>
        /// Returns a copy of this string converted to specific case, using the casing rules of the invariant culture.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="options">The specific case.</param>
        /// <returns>The specific case equivalent of the current string.</returns>
        public static string ToSpecificCaseInvariant(this string source, Cases options)
        {
            return ToSpecificCase(source, options, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Breaks lines.
        /// </summary>
        /// <param name="text">The original string.</param>
        /// <param name="length">The count of a line.</param>
        /// <param name="newLine">The optional newline string.</param>
        /// <returns>A new text with line break.</returns>
        public static string BreakLines(string text, int length, string newLine = null)
        {
            var idx = 0;
            var len = text.Length;
            var str = new StringBuilder();
            while (idx < len)
            {
                if (idx > 0)
                {
                    str.Append(newLine ?? Environment.NewLine);
                }

                if (idx + length >= len)
                {
                    str.Append(text.Substring(idx));
                }
                else
                {
                    str.Append(text.Substring(idx, length));
                }

                idx += length;
            }

            return str.ToString();
        }

        /// <summary>
        /// Breaks lines.
        /// </summary>
        /// <param name="text">The original string.</param>
        /// <param name="length">The count of a line.</param>
        /// <param name="newLine">The newline character.</param>
        /// <returns>A new text with line break.</returns>
        public static string BreakLines(string text, int length, char newLine)
        {
            var idx = 0;
            var len = text.Length;
            var str = new StringBuilder();
            while (idx < len)
            {
                if (idx > 0)
                {
                    str.Append(newLine);
                }

                if (idx + length >= len)
                {
                    str.Append(text.Substring(idx));
                }
                else
                {
                    str.Append(text.Substring(idx, length));
                }

                idx += length;
            }

            return str.ToString();
        }

        /// <summary>
        /// Splits a string into a number of substrings based on the string in an enumerable strings.
        /// </summary>
        /// <param name="source">The source string to split.</param>
        /// <param name="seperator">A string that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="options">System.StringSplitOptions.RemoveEmptyEntries to omit empty array elements from the array returned; or System.StringSplitOptions.None to include empty array elements in the array returned.</param>
        /// <returns>A string enumerable instance whose elements contain the substrings in the source string that are delimited by one or more characters in separator.</returns>
        public static IEnumerable<string> YieldSplit(this string source, string seperator, StringSplitOptions options = StringSplitOptions.None)
        {
            if (options == StringSplitOptions.None && string.IsNullOrEmpty(source)) yield break;
            if (options == StringSplitOptions.RemoveEmptyEntries && string.IsNullOrWhiteSpace(source)) yield break;
            if (string.IsNullOrEmpty(seperator))
            {
                yield return source;
                yield break;
            }

            var index = 0;
            if (options == StringSplitOptions.None)
            {
                while (true)
                {
                    var i = source.IndexOf(seperator, index);
                    if (i < 0)
                    {
                        yield return source.Substring(index);
                        break;
                    }

                    yield return source.Substring(index, i - index);
                    index = i + seperator.Length;
                    if (index >= source.Length) break;
                }
            }
            else
            {
                while (true)
                {
                    var i = source.IndexOf(seperator, index);
                    if (i < 0)
                    {
                        yield return source.Substring(index);
                        break;
                    }

                    if (i <= index) continue;
                    yield return source.Substring(index, i - index);
                    index = i + seperator.Length;
                    if (index >= source.Length) break;
                }
            }
        }

        /// <summary>
        /// Splits a string into a number of substrings based on the character in an enumerable strings.
        /// </summary>
        /// <param name="source">The source string to split.</param>
        /// <param name="seperator">A character that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="options">System.StringSplitOptions.RemoveEmptyEntries to omit empty array elements from the array returned; or System.StringSplitOptions.None to include empty array elements in the array returned.</param>
        /// <returns>A string enumerable instance whose elements contain the substrings in the source string that are delimited by one or more characters in separator.</returns>
        public static IEnumerable<string> YieldSplit(this string source, char seperator, StringSplitOptions options = StringSplitOptions.None)
        {
            return YieldSplit(source, new[] { seperator }, options);
        }

        /// <summary>
        /// Splits a string into a number of substrings based on the characters in an enumerable strings.
        /// </summary>
        /// <param name="source">The source string to split.</param>
        /// <param name="seperatorA">A character that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="seperatorB">Another character that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="options">System.StringSplitOptions.RemoveEmptyEntries to omit empty array elements from the array returned; or System.StringSplitOptions.None to include empty array elements in the array returned.</param>
        /// <returns>A string enumerable instance whose elements contain the substrings in the source string that are delimited by one or more characters in separator.</returns>
        public static IEnumerable<string> YieldSplit(this string source, char seperatorA, char seperatorB, StringSplitOptions options = StringSplitOptions.None)
        {
            return YieldSplit(source, new[] { seperatorA, seperatorB }, options);
        }

        /// <summary>
        /// Splits a string into a number of substrings based on the characters in an enumerable strings.
        /// </summary>
        /// <param name="source">The source string to split.</param>
        /// <param name="seperatorA">The character A that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="seperatorB">The character B that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="seperatorC">The character C that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="options">System.StringSplitOptions.RemoveEmptyEntries to omit empty array elements from the array returned; or System.StringSplitOptions.None to include empty array elements in the array returned.</param>
        /// <returns>A string enumerable instance whose elements contain the substrings in the source string that are delimited by one or more characters in separator.</returns>
        public static IEnumerable<string> YieldSplit(this string source, char seperatorA, char seperatorB, char seperatorC, StringSplitOptions options = StringSplitOptions.None)
        {
            return YieldSplit(source, new[] { seperatorA, seperatorB, seperatorC }, options);
        }

        /// <summary>
        /// Splits a string into a number of substrings based on the characters in an enumerable strings.
        /// </summary>
        /// <param name="source">The source string to split.</param>
        /// <param name="seperatorA">The character A that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="seperatorB">The character B that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="seperatorC">The character C that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="seperatorD">The character D that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="options">System.StringSplitOptions.RemoveEmptyEntries to omit empty array elements from the array returned; or System.StringSplitOptions.None to include empty array elements in the array returned.</param>
        /// <returns>A string enumerable instance whose elements contain the substrings in the source string that are delimited by one or more characters in separator.</returns>
        public static IEnumerable<string> YieldSplit(this string source, char seperatorA, char seperatorB, char seperatorC, char seperatorD, StringSplitOptions options = StringSplitOptions.None)
        {
            return YieldSplit(source, new[] { seperatorA, seperatorB, seperatorC, seperatorD }, options);
        }

        /// <summary>
        /// Splits a string into a number of substrings based on the characters in an enumerable strings.
        /// </summary>
        /// <param name="source">The source string to split.</param>
        /// <param name="seperators">A character array that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="options">System.StringSplitOptions.RemoveEmptyEntries to omit empty array elements from the array returned; or System.StringSplitOptions.None to include empty array elements in the array returned.</param>
        /// <returns>A string enumerable instance whose elements contain the substrings in the source string that are delimited by one or more characters in separator.</returns>
        public static IEnumerable<string> YieldSplit(this string source, char[] seperators, StringSplitOptions options = StringSplitOptions.None)
        {
            if (options == StringSplitOptions.None && string.IsNullOrEmpty(source)) yield break;
            if (options == StringSplitOptions.RemoveEmptyEntries && string.IsNullOrWhiteSpace(source)) yield break;
            if (seperators == null || seperators.Length == 0)
            {
                yield return source;
                yield break;
            }

            var index = 0;
            if (options == StringSplitOptions.None)
            {
                while (true)
                {
                    var i = source.IndexOfAny(seperators, index);
                    if (i < 0)
                    {
                        yield return source.Substring(index);
                        break;
                    }

                    yield return source.Substring(index, i - index);
                    index = i + 1;
                    if (index >= source.Length) break;
                }
            }
            else
            {
                while (true)
                {
                    var i = source.IndexOfAny(seperators, index);
                    if (i < 0)
                    {
                        yield return source.Substring(index);
                        break;
                    }

                    if (i <= index) continue;
                    yield return source.Substring(index, i - index);
                    index = i + 1;
                    if (index >= source.Length) break;
                }
            }
        }

        /// <summary>
        /// Splits a string into a number of substrings based on the strings in an enumerable strings.
        /// </summary>
        /// <param name="source">The source string to split.</param>
        /// <param name="seperators">A string array that delimits the substrings in the source string, an empty array that contains no delimiters, or null.</param>
        /// <param name="options">System.StringSplitOptions.RemoveEmptyEntries to omit empty array elements from the array returned; or System.StringSplitOptions.None to include empty array elements in the array returned.</param>
        /// <returns>A string enumerable instance whose elements contain the substrings in the source string that are delimited by one or more characters in separator.</returns>
        public static IEnumerable<string> YieldSplit(this string source, IEnumerable<string> seperators, StringSplitOptions options = StringSplitOptions.None)
        {
            if (options == StringSplitOptions.None && string.IsNullOrEmpty(source)) yield break;
            if (options == StringSplitOptions.RemoveEmptyEntries && string.IsNullOrWhiteSpace(source)) yield break;
            if (seperators == null)
            {
                yield return source;
                yield break;
            }

            var index = 0;
            if (options == StringSplitOptions.None)
            {
                while (true)
                {
                    var indexes = seperators.Select(ele => (Index: source.IndexOf(ele, index), ele.Length)).Where(ele => ele.Index >= 0).ToList();
                    if (indexes.Count == 0)
                    {
                        yield return source.Substring(index);
                        break;
                    }

                    var i = indexes.Min(ele => ele.Index);
                    var len = indexes.Where(ele => ele.Index == i).Max(ele => ele.Length);
                    yield return source.Substring(index, i - index);
                    index = i + len;
                    if (index >= source.Length) break;
                }
            }
            else
            {
                while (true)
                {
                    var indexes = seperators.Select(ele => (Index: source.IndexOf(ele, index), ele.Length)).Where(ele => ele.Index >= 0).ToList();
                    if (indexes.Count == 0)
                    {
                        yield return source.Substring(index);
                        break;
                    }

                    var i = indexes.Min(ele => ele.Index);
                    if (i <= index) continue;
                    var len = indexes.Where(ele => ele.Index == i).Max(ele => ele.Length);
                    yield return source.Substring(index, i - index);
                    index = i + len;
                    if (index >= source.Length) break;
                }
            }
        }

        /// <summary>
        /// Reads lines from a specific stream reader.
        /// </summary>
        /// <param name="source">The source string to split.</param>
        /// <param name="removeEmptyLine">true if need remove the empty line; otherwise, false.</param>
        /// <returns>Lines from the specific string.</returns>
        public static IEnumerable<string> ReadLines(string source, bool removeEmptyLine = false)
        {
            return YieldSplit(source, new[] { "\r\n", "\n", "\r" }, removeEmptyLine ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
        }

        /// <summary>
        /// Deserializes an object into JSON format.
        /// </summary>
        /// <param name="s">A JSON format string to deserialize.</param>
        /// <returns>An object deserialized.</returns>
        internal static T FromJson<T>(string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            using var stream = new MemoryStream(bytes);
            var serializer = new DataContractJsonSerializer(typeof(T));
            return (T)serializer.ReadObject(stream);
        }

        /// <summary>
        /// Serializes an object into JSON format.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="options">The optional serializer settings.</param>
        /// <returns>A JSON string.</returns>
        internal static string ToJson(object obj, JsonSerializerOptions options = null)
        {
            return ToJson(obj, (o, t) =>
            {
                return JsonSerializer.Serialize(o, t, options);
            });
        }

        /// <summary>
        /// Serializes an object into JSON format.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="options">The optional serializer settings.</param>
        /// <returns>A JSON string.</returns>
        internal static string ToJson(object obj, DataContractJsonSerializerSettings options)
        {
            return ToJson(obj, (o, t) =>
            {
                var serializer = options != null ? new DataContractJsonSerializer(t, options) : new DataContractJsonSerializer(t);
                using var stream = new MemoryStream();
                serializer.WriteObject(stream, o);
                stream.Position = 0;
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);
                return Encoding.UTF8.GetString(bytes);
            });
        }

        /// <summary>
        /// Serializes an object into JSON format.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="converter">The fallback converter.</param>
        /// <returns>A JSON string.</returns>
        private static string ToJson(object obj, Func<object, Type, string> converter)
        {
            if (obj == null) return "null";
            var t = obj.GetType();
            if (obj is JsonDocument jsonDoc)
            {
                return jsonDoc.RootElement.ToString();
            }

            if (obj is JsonElement jsonEle)
            {
                return jsonEle.ToString();
            }

            if (t.FullName.StartsWith("Newtonsoft.Json.Linq.J", StringComparison.InvariantCulture))
            {
                if (t.FullName.Equals("Newtonsoft.Json.Linq.JObject", StringComparison.InvariantCulture)
                    || t.FullName.Equals("Newtonsoft.Json.Linq.JArray", StringComparison.InvariantCulture))
                    return obj.ToString();
            }

            if (obj is Security.TokenRequest tokenReq)
            {
                return tokenReq.ToJsonString();
            }

            if (obj is IEnumerable<KeyValuePair<string, string>> col)
            {
                var str = new StringBuilder("{");
                foreach (var kvp in col)
                {
                    str.AppendFormat("\"{0}\":\"{1}\",", ToStringJsonToken(kvp.Key), ToStringJsonToken(kvp.Value));
                }

                str.Remove(str.Length - 1, 1);
                str.Append("}");
                return str.ToString();
            }

            if (t == typeof(string)) return ToStringJsonToken(obj.ToString());
            if (obj is Net.HttpUri uri2) return ToStringJsonToken(uri2.ToString());
            if (obj is Uri uri)
            {
                try
                {
                    return ToStringJsonToken(uri.OriginalString);
                }
                catch (InvalidOperationException)
                {
                    return ToStringJsonToken(uri.ToString());
                }
            }

            if (t.IsValueType)
            {
                if (obj is bool b)
                    return b ? "true" : "false";
                if (obj is int i32)
                    return i32.ToString("g", CultureInfo.InvariantCulture);
                if (obj is long i64)
                    return i64.ToString("g", CultureInfo.InvariantCulture);
                if (obj is float f1)
                    return f1.ToString("g", CultureInfo.InvariantCulture);
                if (obj is uint i32u)
                    return i32u.ToString("g", CultureInfo.InvariantCulture);
                if (obj is uint i64u)
                    return i64u.ToString("g", CultureInfo.InvariantCulture);
                if (obj is DateTime d)
                    return ToStringJsonToken(d.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                if (obj is DateTimeOffset dto)
                    return ToStringJsonToken(dto.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"));
                if (obj is double f2)
                {
                    var str = f2.ToString("g", CultureInfo.InvariantCulture);
                    if (str.IndexOf("e") < 0) return str;
                    return ToStringJsonToken(str);
                }

                if (obj is TimeSpan ts)
                {
                    var str = ts.TotalSeconds.ToString("g", CultureInfo.InvariantCulture);
                    if (str.IndexOf("e") < 0) return str;
                    return ToStringJsonToken(str);
                }

                if (t == typeof(Guid)
                    || t == typeof(Maths.Angle))
                    return ToStringJsonToken(obj.ToString());
                if (t == typeof(byte)
                    || t == typeof(short)
                    || t == typeof(ushort))
                    return obj.ToString();
            }

            if (t == typeof(DBNull)) return "null";
            return converter(obj, t);
        }

        internal static bool ReplaceBackSlash(StringBuilder sb, StringBuilder backSlash, char c)
        {
            if (backSlash.Length == 0)
            {
                switch (c)
                {
                    case 'x':
                    case 'X':
                    case 'u':
                    case 'U':
                        backSlash.Append(c);
                        return false;
                    case 'R':
                    case 'r':
                        sb.Append('\r');
                        break;
                    case 'N':
                    case 'n':
                        sb.Append('\n');
                        break;
                    case 'A':
                    case 'a':
                        sb.Append('\a');
                        break;
                    case 'B':
                    case 'b':
                        sb.Append('\b');
                        break;
                    case 'T':
                    case 't':
                        sb.Append('\t');
                        break;
                    case 'F':
                    case 'f':
                        sb.Append('\f');
                        break;
                    case 'V':
                    case 'v':
                        sb.Append('\v');
                        break;
                    case '0':
                        sb.Append('\0');
                        break;
                    default:
                        sb.Append(c);
                        break;
                }

                return true;
            }

            var firstBackSlash = backSlash[0];
            int len;
            if (firstBackSlash == 'x' || firstBackSlash == 'X')
            {
                len = 3;
            }
            else if (firstBackSlash == 'u' || firstBackSlash == 'U')
            {
                len = 5;
            }
            else
            {
                return true;
            }

            if (backSlash.Length < len)
            {
                backSlash.Append(c);
                return false;
            }

            try
            {
                var num = Convert.ToInt32(backSlash.ToString().Substring(1), 16);
                sb.Append(char.ConvertFromUtf32(num));
            }
            catch (FormatException)
            {
                sb.Append(backSlash.ToString());
            }
            catch (ArgumentException)
            {
                sb.Append(backSlash.ToString());
            }

            return true;
        }

        internal static string ReplaceBackSlash(IEnumerable<char> s)
        {
            var sb = new StringBuilder();
            StringBuilder backSlash = null;
            foreach (var c in s)
            {
                if (c == '\\')
                {
                    backSlash = new StringBuilder();
                    continue;
                }

                if (backSlash != null)
                {
                    if (ReplaceBackSlash(sb, backSlash, c)) backSlash = null;
                    continue;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        internal static string ToStringJsonToken(string s, bool removeQuotes = false)
        {
            if (s == null) return removeQuotes ? null : "null";
            s = s
                .Replace("\\", "\\\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("\a", "\\n")
                .Replace("\b", "\\t")
                .Replace("\f", "\\f")
                .Replace("\v", "\\v")
                .Replace("\0", "\\0")
                .Replace("\"", "\\\"");
            if (!removeQuotes) s = string.Format("\"{0}\"", s);
            return s;
        }

        private static string ToUpper(string source, CultureInfo culture)
        {
            return culture == null ? source.ToUpper() : source.ToUpper(culture);
        }

        private static string ToLower(string source, CultureInfo culture)
        {
            return culture == null ? source.ToLower() : source.ToLower(culture);
        }
    }
}
