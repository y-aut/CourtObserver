﻿using System.Text.RegularExpressions;

namespace COLib
{
    /// <summary>
    /// Slack のユーザーを表現する構造体です。
    /// </summary>
    public struct SlackUser
    {
        /// <summary>
        /// ユーザー ID です。10桁の半角英数字で表されます。
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// インスタンスを初期化します。
        /// </summary>
        public SlackUser(string id)
        {
            if (!IsValidID(id))
            {
                throw new InvalidDataException();
            }
            ID = id;
        }

        /// <summary>
        /// 文字列が有効な Slack の ID かどうかを判定します。
        /// </summary>
        public static bool IsValidID(string id)
        {
            return Regex.IsMatch(id, "^[0-9A-Z]{10}$");
        }
    }
}
