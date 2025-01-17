﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Text;
using System.Text.Json;

namespace Trivial.Web
{
    /// <summary>
    /// Web format utility.
    /// </summary>
    public static partial class WebFormat
    {
        /// <summary>
        /// Parses JavaScript date tick to date and time.
        /// </summary>
        /// <param name="tick">The JavaScript date tick.</param>
        /// <returns>A date and time.</returns>
        public static DateTime ParseDate(long tick)
        {
            var time = new DateTime(tick * 10000 + 621355968000000000, DateTimeKind.Utc);
            return time.ToLocalTime();
        }

        /// <summary>
        /// Parses JavaScript date tick to date and time.
        /// </summary>
        /// <param name="tick">The JavaScript date tick.</param>
        /// <returns>A date and time.</returns>
        public static DateTime? ParseDate(long? tick)
        {
            if (!tick.HasValue) return null;
            return ParseDate(tick.Value);
        }

        /// <summary>
        /// Parses JavaScript date tick to date and time back.
        /// </summary>
        /// <param name="date">A date and time.</param>
        /// <returns>The JavaScript date tick.</returns>
        public static long ParseDate(DateTime date)
        {
            return (date.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        }

        /// <summary>
        /// Parses JavaScript date tick to date and time back.
        /// </summary>
        /// <param name="date">A date and time.</param>
        /// <returns>The JavaScript date tick.</returns>
        public static long ParseDate(DateTimeOffset date)
        {
            return (date.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        }

        /// <summary>
        /// Parses JavaScript date tick to date and time back.
        /// </summary>
        /// <param name="date">A date and time.</param>
        /// <returns>The JavaScript date tick.</returns>
        public static long? ParseDate(DateTime? date)
        {
            if (!date.HasValue) return null;
            return ParseDate(date.Value);
        }

        /// <summary>
        /// Parses JavaScript date tick to date and time back.
        /// </summary>
        /// <param name="date">A date and time.</param>
        /// <returns>The JavaScript date tick.</returns>
        public static long? ParseDate(DateTimeOffset? date)
        {
            if (!date.HasValue) return null;
            return ParseDate(date.Value);
        }

        /// <summary>
        /// Parses JavaScript date JSON string to date and time.
        /// </summary>
        /// <param name="tick">The JSON token value of JavaScript date.</param>
        /// <returns>A date and time.</returns>
        public static DateTime? ParseDate(string tick)
        {
            if (string.IsNullOrWhiteSpace(tick)) return null;
            tick = tick.Trim().ToUpperInvariant();
            if (tick.Length == 8)
            {
                var y2 = GetNaturalNumber(tick, 0, 4);
                if (y2 < 0) return null;
                var m2 = GetNaturalNumber(tick, 4, 2);
                if (m2 < 0) return null;
                var d2 = GetNaturalNumber(tick, 6);
                if (d2 < 0) return null;
                return new DateTime(y2, m2, d2, 0, 0, 0, DateTimeKind.Utc);
            }

            if (tick.Length < 10 || tick[4] != '-') return null;
            var y = GetNaturalNumber(tick, 0, 4);
            if (y < 0) return null;
            var pos = tick[7] == '-' ? 8 : 7;
            var m = GetNaturalNumber(tick, 5, 2);
            if (m < 0)
            {
                if (tick[6] == '-') m = GetNaturalNumber(tick, 5, 1);
                if (m < 0) return null;
            }

            var d = GetNaturalNumber(tick, pos, 2);
            if (d < 1)
            {
                pos += 4;
                d = GetNaturalNumber(tick, pos, 1);
                if (d < 1) return null;
            }
            else
            {
                pos += 3;
            }

            var date = new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc);
            if (pos >= tick.Length) return date;
            tick = tick.Substring(pos);
            var arr = tick.Split(':');
            if (arr.Length < 2) return date;
            if (!int.TryParse(arr[0], out var h)) return date;
            if (!int.TryParse(arr[1], out var mm)) return date;
            var t = new DateTime(y, m, d, h, mm, 0, DateTimeKind.Utc).ToLocalTime();
            if (arr.Length == 2) return t;
            if (arr.Length == 3)
            {
                if (!double.TryParse(arr[2].Replace("Z", string.Empty), out var sf)) return t;
                return t.AddSeconds(sf);
            }

            if (arr[2].Length < 5)
            {
                var sf = GetNaturalNumber(arr[2], 0, 2);
                return sf > 0 ? t.AddSeconds(sf) : t;
            }

            var s = GetNaturalNumber(arr[2], 0, 2);
            if (s < 0 || !int.TryParse(arr[3], out var rm)) return t;
            var neg = arr[2][2] == '-' ? 1 : -1;
            var hasSep = (neg == 1) || (arr[2][2] == '+');
            var rh = GetNaturalNumber(arr[2], hasSep ? 3 : 2);
            return t.AddSeconds(s).AddMinutes(neg * rm).AddHours(neg * rh);
        }

        /// <summary>
        /// Encodes a specific byte array into Base64Url format.
        /// </summary>
        /// <param name="bytes">The value to encode.</param>
        /// <returns>A Base64Url string.</returns>
        public static string Base64UrlEncode(byte[] bytes)
        {
            if (bytes == null) return null;
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", string.Empty);
        }

        /// <summary>
        /// Encodes a specific string into Base64Url format.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <param name="encoding">Optional text encoding.</param>
        /// <returns>A Base64Url string.</returns>
        public static string Base64UrlEncode(string value, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var bytes = (encoding ?? Encoding.UTF8).GetBytes(value);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", string.Empty);
        }

        /// <summary>
        /// Encodes a specific object into JSON Base64Url format.
        /// </summary>
        /// <param name="obj">The object to encode.</param>
        /// <returns>A Base64Url string.</returns>
        public static string Base64UrlEncode(object obj)
        {
            if (obj == null) return string.Empty;
            var t = obj.GetType();
            if (t == typeof(string)) return Base64UrlEncode(obj.ToString());
            return Base64UrlEncode(Text.StringExtensions.ToJson(obj));
        }

        /// <summary>
        /// Decodes the string from a Base64Url format.
        /// </summary>
        /// <param name="s">A Base64Url encoded string.</param>
        /// <returns>A plain text.</returns>
        public static byte[] Base64UrlDecode(string s)
        {
            if (s == null) return null;
            if (s == string.Empty) return new byte[0];
            s = s.Replace("-", "+").Replace("_", "/");
            var rest = s.Length % 4;
            if (rest > 0) s = s.PadRight(4 - rest + s.Length, '=');
            var bytes = Convert.FromBase64String(s);
            return bytes;
        }

        /// <summary>
        /// Decodes the string from a Base64Url format.
        /// </summary>
        /// <param name="s">A Base64Url encoded string.</param>
        /// <param name="encoding">Optional text encoding.</param>
        /// <returns>A plain text.</returns>
        public static string Base64UrlDecodeToString(string s, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var bytes = Base64UrlDecode(s);
            return (encoding ?? Encoding.ASCII).GetString(bytes);
        }

        /// <summary>
        /// Decodes and deserializes the object from a JSON Base64Url format.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="s">A Base64Url encoded string.</param>
        /// <returns>The object typed.</returns>
        public static T Base64UrlDecodeTo<T>(string s)
        {
            if (string.IsNullOrEmpty(s)) return default;
            var bytes = Base64UrlDecode(s);
            var d = GetJsonDeserializer<T>();
            if (d != null) return d(s);
            var serializer = new DataContractJsonSerializer(typeof(T));
            using var stream = new MemoryStream(bytes);
            return (T)serializer.ReadObject(stream);
        }

        /// <summary>
        /// Gets the JSON deserializer.
        /// </summary>
        /// <typeparam name="T">The type of the instance.</typeparam>
        /// <returns>A function for deserialization.</returns>
        internal static Func<string, T> GetJsonDeserializer<T>()
        {
            var t = typeof(T);
            if (t == typeof(JsonDocument))
            {
                return str =>
                {
                    return (T)(object)JsonDocument.Parse(str);
                };
            }
            else if (t.FullName.StartsWith("Newtonsoft.Json.Linq.J", StringComparison.InvariantCulture))
            {
                try
                {
                    var parser = t.GetMethod("Parse", new[] { typeof(string) });
                    if (parser != null && parser.IsStatic)
                    {
                        return str =>
                        {
                            return (T)parser.Invoke(null, new object[] { str });
                        };
                    }
                }
                catch (AmbiguousMatchException)
                {
                }
                catch (ArgumentException)
                {
                }
            }
            else if (t == typeof(string))
            {
                return str => (T)(object)str;
            }

            return null;
        }

        /// <summary>
        /// Tries to get the integer from a part of the specific string.
        /// </summary>
        /// <param name="s">A specific string.</param>
        /// <param name="start">The start index of the string to get the integer.</param>
        /// <param name="len">The length to get.</param>
        /// <returns>A natural number; or -1, if failed.</returns>
        private static int GetNaturalNumber(string s, int start, int? len = null)
        {
            const uint ZERO = '0';
            var end = len.HasValue ? Math.Min(start + len.Value, s.Length) : s.Length;
            uint n = 0;
            for (var i = start; i < end; i++)
            {
                var c = s[i];
                var j = c - ZERO;
                if (j > 9) return -1;
                n = n * 10 + j;
            }

            return (int)n;
        }
    }
}
