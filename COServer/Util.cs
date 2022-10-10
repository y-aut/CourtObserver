using Microsoft.AspNetCore.Mvc;
using System.Text;
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
        private static ILogger? _logger;

        /// <summary>
        /// 共用のロガーオブジェクトです。
        /// </summary>
        public static ILogger Logger
        {
            set
            {
                _logger = value;
            }
            get
            {
                if (_logger == null)
                {
                    throw new NullReferenceException();
                }
                return _logger;
            }
        }

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
            return GetTFromPath(dict, path, GetValueAsString, _ => null);
        }

        /// <summary>
        /// "key1/0/./2/key2" のようなパスに対応する値を JsonDict として取り出します。
        /// 取り出せない場合は、null が返されます。
        /// </summary>
        public static JsonDict? GetJsonDictFromPath(this JsonDict dict, string path)
        {
            return GetTFromPath(dict, path, GetValueAsJsonDict, i => i);
        }

        /// <summary>
        /// "key1/0/./2/key2" のようなパスに対応する値を JsonDict のリストとして取り出します。
        /// 取り出せない場合は、空のリストが返されます。
        /// </summary>
        public static List<JsonDict> GetListOfJsonDictFromPath(this JsonDict dict, string path)
        {
            return GetTFromPath(dict, path, GetValueAsListOfJsonDict, _ => null) ?? new();
        }

        private static T? GetTFromPath<T>(this JsonDict dict, string path,
            Func<JsonDict, string, T?> finish, Func<JsonDict, T?> handler)
        {
            if (path.Length == 0)
            {
                return default;
            }
            
            var keys = path.Split("/");
            var current = dict;

            int i;
            for (i = 0; i < keys.Length - 1; i++)
            {
                // 次が数字なら配列
                if (int.TryParse(keys[i + 1], out int index))
                {
                    current = current.GetValueAsJsonDict(keys[i++], index);
                }
                else
                {
                    current = current.GetValueAsJsonDict(keys[i]);
                }
                if (current == null)
                {
                    return default;
                }
            }

            // パスの最後が数字なら JsonDict で確定
            if (i == keys.Length)
            {
                return handler(current);
            }
            return finish(current, keys.Last());
        }

        /// <summary>
        /// 任意のオブジェクトを JsonElement に変換します。
        /// </summary>
        public static JsonElement ToJsonElement(this object? dict)
        {
            return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(dict));
        }

        /// <summary>
        /// JsonDict 中の指定したパスにある値を新しい値で置き換えます。
        /// 置き換えに失敗した場合は、null が返されます。
        /// </summary>
        public static JsonDict? Replace(this JsonDict dict, string path, object value)
        {
            return dict.ReplaceInner(path, value.ToJsonElement());
        }

        private static JsonDict? ReplaceInner(this JsonDict dict, string path, JsonElement? value)
        {
            if (value == null)
            {
                return null;
            }

            var last = path.LastIndexOf('/');
            if (last == -1)
            {
                dict[path] = value.Value;
                return dict;
            }

            var hd = path[..last];
            var tl = path[(last + 1)..];

            if (int.TryParse(tl, out int index))
            {
                var hdDicts = dict.GetListOfJsonDictFromPath(hd);
                var res = JsonSerializer.Deserialize<JsonDict>(value.Value);
                if (res == null)
                {
                    return null;
                }
                hdDicts[index] = res;
                return dict.ReplaceInner(hd, hdDicts.ToJsonElement());
            }
            else
            {
                var hdDict = dict.GetJsonDictFromPath(hd);
                if (hdDict == null)
                {
                    return null;
                }
                hdDict[tl] = value.Value;
                return dict.ReplaceInner(hd, hdDict.ToJsonElement());
            }
        }

        /// <summary>
        /// Json 文字列のパスで示す値の中に新しいキーと値の組を加えます。
        /// 失敗した場合は、null が返されます。
        /// </summary>
        public static JsonDict? AddJson(this JsonDict dict, string path, string key, object value)
        {
            var target = dict.GetJsonDictFromPath(path);
            if (target == null)
            {
                return null;
            }
            if (!target.ContainsKey(key))
            {
                target.Add(key, value.ToJsonElement());
            }
            return dict.Replace(path, target);
        }

        /// <summary>
        /// Json 文字列のパスで示す値の中のキーと値の組を削除します。
        /// 失敗した場合は、null が返されます。
        /// </summary>
        public static JsonDict? RemoveJson(this JsonDict dict, string path, string key)
        {
            var target = dict.GetJsonDictFromPath(path);
            if (target == null)
            {
                return null;
            }
            if (target.ContainsKey(key))
            {
                target.Remove(key);
            }
            return dict.Replace(path, target);
        }
    }
}
