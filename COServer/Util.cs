using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace COServer
{
    using JsonDict = Dictionary<string, JsonElement>;

    /// <summary>
    /// 静的メソッドを扱うクラスです。
    /// </summary>
    public static partial class Util
    {
        /// <summary>
        /// 共用のロガーオブジェクトです。
        /// </summary>
        public static ILogger? Logger { get; set; }

        /// <summary>
        /// ログを出力して ObjectResult を送信します。
        /// </summary>
        public static ObjectResult Warn(this ILogger logger, ObjectResult result)
        {
            logger.LogWarning("API Responsed: {value}", result.Value?.ToString());
            return result;
        }

        /// <summary>
        /// 例外を Warning 出力します。
        /// </summary>
        public static void Warn(this ILogger logger, Exception e)
        {
            logger.LogWarning("API Exception Occured: {value}", e.ToString());
        }

        /// <summary>
        /// <see cref="GetValueDot"/> でエラーにならないかを検査します。
        /// </summary>
        public static bool ContainsKeyDot<T>(this Dictionary<string, T> dict, string key)
        {
            if (key == ".")
            {
                return dict.Count == 1;
            }
            return dict.ContainsKey(key);
        }

        /// <summary>
        /// 値が 1つしかないとき、キーが "." ならばその値を返します。
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static T GetValueDot<T>(this Dictionary<string, T> dict, string key)
        {
            if (key == ".")
            {
                if (dict.Count == 1)
                {
                    return dict.First().Value;
                }
                else
                {
                    throw new ArgumentException("The key is dot, but there are two or more values.");
                }
            }
            return dict[key];
        }

        /// <summary>
        /// キーに対応する値を文字列として取り出します。
        /// 文字列でない場合は、null が返されます。
        /// </summary>
        public static string? GetValueAsString(this JsonDict dict, string key)
        {
            if (!dict.ContainsKeyDot(key) || dict.GetValueDot(key).ValueKind != JsonValueKind.String)
            {
                return null;
            }
            return dict.GetValueDot(key).GetString();
        }

        /// <summary>
        /// キーに対応する値をブール値として取り出します。
        /// ブール値でない場合は、null が返されます。
        /// </summary>
        public static bool? GetValueAsBool(this JsonDict dict, string key)
        {
            if (!dict.ContainsKeyDot(key))
            {
                return null;
            }
            if (dict.GetValueDot(key).ValueKind == JsonValueKind.True)
            {
                return true;
            }
            else if (dict.GetValueDot(key).ValueKind == JsonValueKind.False)
            {
                return false;
            }
            return null;
        }

        /// <summary>
        /// キーに対応する値を JsonDict として取り出します。
        /// キーにドットを指定した場合、要素が 1つしかなければ、その要素にマッチします。
        /// 変換できない場合は、null が返されます。
        /// </summary>
        public static JsonDict? GetValueAsJsonDict(this JsonDict dict, string key)
        {
            try
            {
                return JsonSerializer.Deserialize<JsonDict>(dict.GetValueDot(key));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// キーに対応するリストから指定した要素を JsonDict として取り出します。
        /// 変換できない場合は、null が返されます。
        /// </summary>
        public static JsonDict? GetValueAsJsonDict(this JsonDict dict, string key, int index)
        {
            if (!dict.ContainsKeyDot(key) || dict.GetValueDot(key).ValueKind != JsonValueKind.Array)
            {
                try
                {
                    return JsonSerializer.Deserialize<JsonDict>(dict.GetValueDot(key));
                }
                catch (Exception)
                {
                    return null;
                }
            }
            try
            {
                return JsonSerializer.Deserialize<JsonDict>(dict.GetValueDot(key)[index]);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// キーに対応する値を JsonDict のリストとして取り出します。
        /// 変換できない場合は、空のリストが返されます。
        /// </summary>
        public static List<JsonDict> GetValueAsListOfJsonDict(this JsonDict dict, string key)
        {
            if (!dict.ContainsKeyDot(key) || dict.GetValueDot(key).ValueKind != JsonValueKind.Array)
            {
                try
                {
                    var item = JsonSerializer.Deserialize<JsonDict>(dict.GetValueDot(key)) ?? new();
                    return new List<JsonDict>() { item };
                }
                catch (Exception)
                {
                    return new List<JsonDict>();
                }
            }
            var list = new List<JsonDict>();
            for (int i = 0; i < dict.GetValueDot(key).GetArrayLength(); i++)
            {
                try
                {
                    var item = JsonSerializer.Deserialize<JsonDict>(dict.GetValueDot(key)[i]) ?? new();
                    list.Add(item);
                }
                catch (Exception)
                {
                    return new();
                }
            }
            return list;
        }

        /// <summary>
        /// "key1/0/./2/key2" のようなパスに対応する値を文字列として取り出します。
        /// 取り出せない場合は、null が返されます。
        /// </summary>
        public static string? GetStringFromPath(this JsonDict dict, string path)
        {
            if (path.Length == 0)
            {
                return null;
            }
            
            var keys = path.Split("/");
            var current = dict;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                // 次が数字なら配列
                if (int.TryParse(keys[i + 1], out int index))
                {
                    current = dict.GetValueAsJsonDict(keys[i], index);
                }
                else
                {
                    current = dict.GetValueAsJsonDict(keys[i]);
                }
                if (current == null)
                {
                    return null;
                }
            }

            return current.GetValueAsString(keys.Last());
        }
    }
}
