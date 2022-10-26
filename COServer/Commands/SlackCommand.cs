using COLib;
using System.Text.RegularExpressions;

namespace COServer.Commands
{
    /// <summary>
    /// Slack スラッシュコマンドを扱うクラスです。
    /// </summary>
    public abstract class SlackCommand
    {
        /// <summary>
        /// コマンドの名前です。
        /// </summary>
        protected string Name { get; init; }

        /// <summary>
        /// コマンドパラメータです。
        /// タブや改行、スペースの重複は削除され、小文字に変換したものが格納されます。
        /// </summary>
        protected string Text { get; init; }

        /// <summary>
        /// 利用しているユーザーです。
        /// </summary>
        protected SlackUser User { get; init; }

        /// <summary>
        /// レスポンス URL です。
        /// </summary>
        protected Uri ResponseUrl { get; init; }

        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// <param name="name">コマンド名。</param>
        /// <param name="text">コマンドパラメータ。</param>
        /// <param name="user">利用しているユーザー。</param>
        /// <param name="url">レスポンス URL。</param>
        public SlackCommand(string name, string? text, SlackUser user, Uri url)
        {
            Name = name;
            Text = Regex.Replace(
                text?.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ') ?? "", " +", " ")
                .ToLower();
            User = user;
            ResponseUrl = url;
        }

        /// <summary>
        /// コマンドの使用方法を表す文字列を取得します。
        /// </summary>
        protected abstract string GetUsage();

        /// <summary>
        /// コマンドの実際の処理を実装します。
        /// </summary>
        /// <param name="message">出力メッセージ。</param>
        /// <returns>コマンドが正常に実行されたかどうか。</returns>
        protected abstract bool ExecuteImpl(out string message);

        /// <summary>
        /// コマンド共通の処理を実装します。
        /// </summary>
        /// <param name="message">出力メッセージ。</param>
        /// <returns>コマンドが正常に実行されたかどうか。</returns>
        public bool Execute(out string message)
        {
            if (Text.Any())
            {
                message = $"`/{Name} {Text}`\n\n";
            }
            else
            {
                message = $"`/{Name}`\n\n";
            }

            if (Text == "help")
            {
                message += GetUsage();
                return true;
            }

            if (ExecuteImpl(out var result))
            {
                message += result;
                return true;
            }
            else
            {
                message += result;
                message += $"\n\n使用方法を表示するには `/{Name} help` を実行してください。";
                return false;
            }
        }
    }
}
