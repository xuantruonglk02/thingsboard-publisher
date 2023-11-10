using Cysharp.Text;
using ThingsBoardPublisher.Configurations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Globalization;
using System.Text;

namespace ThingsBoardPublisher
{
    public static class Extentions
    {
        private const char Hyphen = '-';
        private const int MaxLength = 1000;
        private static CultureInfo cultureInfo = new CultureInfo("en-US");

        public static string Slugify(this string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return text;
            }

            var appendHyphen = false;
            var normalizedText = text.Normalize(NormalizationForm.FormKD);

            using var slug = ZString.CreateStringBuilder();

            for (var i = 0; i < normalizedText.Length; i++)
            {
                var currentChar = Char.ToLowerInvariant(normalizedText[i]);

                if (CharUnicodeInfo.GetUnicodeCategory(currentChar) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (Char.IsLetterOrDigit(currentChar))
                {
                    slug.Append(currentChar);

                    appendHyphen = true;
                }
                else if (currentChar is Hyphen)
                {
                    if (appendHyphen && i != normalizedText.Length - 1)
                    {
                        slug.Append(currentChar);
                        appendHyphen = false;
                    }
                }
                else if (currentChar == '_' || currentChar == '~')
                {
                    slug.Append(currentChar);
                }
                else
                {
                    if (appendHyphen)
                    {
                        slug.Append(Hyphen);

                        appendHyphen = false;
                    }
                }
            }

            var length = Math.Min(slug.Length - GetTrailingHyphenCount(slug.AsSpan()), MaxLength);

            return new string(slug.AsSpan()[..length]).Normalize(NormalizationForm.FormC);
        }

        private static int GetTrailingHyphenCount(ReadOnlySpan<char> input)
        {
            var hyphenCount = 0;
            for (var i = input.Length - 1; i >= 0; i--)
            {
                if (input[i] != Hyphen)
                {
                    break;
                }

                ++hyphenCount;
            }

            return hyphenCount;
        }

        public static string FlattenException(this Exception exception)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {
                stringBuilder.AppendLine(exception?.InnerException?.Message ?? exception.Message);
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.Source);
                stringBuilder.AppendLine(exception.StackTrace);
                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }

        public static string GetSectionValue(this IConfiguration configuration, string key)
        {
            return configuration.GetSection(key).Value;
        }

        public static DateTime NowVN(this DateTime now)
        {
            return now.AddHours(7);
        }

        public static ExpandoObject ToExpandoObject(this Dictionary<string, object> dictionary)
        {
            var expando = new ExpandoObject();

            foreach (var kvp in dictionary)
            {
                expando.TryAdd(kvp.Key, kvp.Value);
            }

            return expando;
        }

        public static Dictionary<string, object> ToTimestampObject(this object obj)
        {
            var dictionary = JObject.FromObject(obj).ToObject<Dictionary<string, object>>();
            if (dictionary.ContainsKey("timestamp") || dictionary.ContainsKey("TIMESTAMP"))
            {
                var timestamp = dictionary.GetValueOrDefault("timestamp") ?? dictionary.GetValueOrDefault("TIMESTAMP");
                dictionary.Remove("timestamp");
                dictionary.Remove("TIMESTAMP");
                var dateStr = timestamp?.ToString() ?? "";
                try
                {
                    var dateParse = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", null);
                    dictionary.Add("timestamp", dateStr);
                    dictionary.Add("ts", dateParse.ToUnixTimestamp());
                }
                catch (Exception)
                {
                    dictionary.Add("timestamp", dateStr);
                    dictionary.Add("ts", DateTime.UtcNow.ToUnixTimestamp());
                }

            }
            else
            {
                dictionary.Add("ts", DateTime.UtcNow.ToUnixTimestamp());
            }

            var newDictionary = new Dictionary<string, object>(dictionary);
            double numberDouble = 0;
            int numberInteger = 0;
            bool isNumber = false;
            foreach (var kvp in dictionary)
            {
                if (kvp.Key != "ts")
                {
                    var vl = kvp.Value.ToString().Trim();
                    isNumber = int.TryParse(vl, NumberStyles.Number, cultureInfo, out numberInteger);
                    if (isNumber)
                    {
                        newDictionary.Remove(kvp.Key);
                        newDictionary.Add(kvp.Key, numberInteger);
                    }
                    else
                    {
                        isNumber = double.TryParse(vl, NumberStyles.Number, cultureInfo, out numberDouble);
                        if (isNumber)
                        {
                            newDictionary.Remove(kvp.Key);
                            newDictionary.Add(kvp.Key, numberDouble);
                        }
                    }

                    // check null
                    if (vl.Equals("NULL") || vl.Equals("null") || vl.Equals("") || vl.Equals("NAN"))
                    {
                        newDictionary.Remove(kvp.Key);
                        newDictionary.Add(kvp.Key, null);
                    }
                }
            }

            return newDictionary;
        }

        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static long ToUnixTimestamp(this DateTime dateTime)
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var zone = 0;
            int.TryParse(AppConfigurations.GetSectionValue("PushSetting:Zone"), out zone);
            var _dateTime = zone > 0 ? dateTime.AddHours(-zone) : dateTime.AddHours(Math.Abs(zone));
            TimeSpan timeSpan = _dateTime - unixEpoch;
            return (long)timeSpan.TotalMilliseconds;
        }

        public static string ToBase64String(this object obj)
        {
            string jsonString = JsonConvert.SerializeObject(obj);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            string base64String = Convert.ToBase64String(jsonBytes);
            return base64String;
        }

        public static T FromBase64String<T>(this string base64String)
        {
            byte[] bytes = Convert.FromBase64String(base64String);
            string json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
