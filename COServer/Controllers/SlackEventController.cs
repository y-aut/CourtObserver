using COLib;
using COServer.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace COServer.Controllers
{
    using JsonDict = Dictionary<string, JsonElement>;
    using StringDict = Dictionary<string, string>;

    /// <summary>
    /// Slack App の操作時に発生するイベントを扱います。
    /// </summary>
    [ApiController]
    [Route("Events")]
    public class SlackEventController : ControllerBase
    {
        /// <summary>
        /// API 認証トークンです。
        /// </summary>
        private const string VERIFICATION_TOKEN = "a7nb2BZhZ8LVNpmnQkS4hxM7";

        private readonly ILogger<SlackEventController> _logger;

        /// <summary>
        /// ユーザー毎のセッション ID です。
        /// </summary>
        private static readonly Dictionary<string, int> sessionId = new();

        /// <summary>
        /// 指定したユーザーのセッション ID を取得します。
        /// </summary>
        public static int GetUserSessionId(SlackUser user)
        {
            lock (sessionId)
            {
                if (sessionId.TryGetValue(user.ID, out var val))
                {
                    return val;
                }
                return 0;
            }
        }

        /// <summary>
        /// 指定したユーザーの新しいセッション ID を取得します。
        /// </summary>
        private static int GetUserNextSessionId(SlackUser user)
        {
            if (!sessionId.ContainsKey(user.ID))
            {
                sessionId.Add(user.ID, 0);
                return 0;
            }
            return ++sessionId[user.ID];
        }

        /// <summary>
        /// インスタンスを初期化します。
        /// </summary>
        public SlackEventController(ILogger<SlackEventController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Slack イベントの処理を行います。
        /// </summary>
        /// <param name="payload">イベント API の本文です。</param>
        [HttpPost]
        public IActionResult PostEvent([FromBody] JsonDict? payload)
        {
            try
            {
                _logger.LogInformation("Slack event received: {payload}", JsonSerializer.Serialize(payload));

                if (payload == null)
                {
                    return _logger.Warn(BadRequest("This content cannnot be resolved."));
                }

                // トークン認証
                if (!VerifyToken(payload, out var result))
                {
                    return result;
                }

                var type = payload.GetValueAsString("type");
                if (type == null)
                {
                    return _logger.Warn(BadRequest("The content must have a \"type\" string."));
                }

                return type switch
                {
                    "url_verification" => VerifyUrl(payload),
                    "event_callback" => HandleEvent(payload),
                    _ => _logger.Warn(BadRequest($"Invalid type string: {type}")),
                };
            }
            catch (Exception e)
            {
                _logger.Warn(e);
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = e.ToString(),
                    ContentType = "text/plain",
                };
            }
        }

        /// <summary>
        /// トークン認証を行います。
        /// </summary>
        private bool VerifyToken(JsonDict content, out IActionResult result)
        {
            var token = content.GetValueAsString("token");
            if (token == null)
            {
                result = _logger.Warn(BadRequest("Illegal content."));
                return false;
            }

            if (token != VERIFICATION_TOKEN)
            {
                result = _logger.Warn(Unauthorized("Invalid token."));
                return false;
            }

            result = Ok();
            return true;
        }

        /// <summary>
        /// トークン認証を行います。
        /// </summary>
        private bool VerifyToken(StringDict content, out IActionResult result)
        {
            if (!content.TryGetValue("token", out var token))
            {
                result = _logger.Warn(BadRequest("Illegal content."));
                return false;
            }

            if (token != VERIFICATION_TOKEN)
            {
                result = _logger.Warn(Unauthorized("Invalid token."));
                return false;
            }

            result = Ok();
            return true;
        }

        /// <summary>
        /// URL 認証を行います。
        /// </summary>
        private IActionResult VerifyUrl(JsonDict content)
        {
            var challenge = content.GetValueAsString("challenge");
            if (challenge == null)
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }

            return Ok(challenge);
        }

        /// <summary>
        /// イベントを処理します。
        /// </summary>
        private IActionResult HandleEvent(JsonDict content)
        {
            var dict = content.GetValueAsJsonDict("event");
            if (dict == null)
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }
            var type = dict.GetValueAsString("type");
            return type switch
            {
                "app_home_opened" => AppHomeOpened(dict),
                _ => _logger.Warn(BadRequest($"Invalid event type string: {type}")),
            };
        }

        /// <summary>
        /// アプリのホーム画面が開かれた時の処理を行います。
        /// </summary>
        private IActionResult AppHomeOpened(JsonDict content)
        {
            var userId = content.GetValueAsString("user");
            if (userId == null || !SlackUser.IsValidID(userId))
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }
            var user = new SlackUser(userId);

            // 選択されている日付を取得
            var view = content.GetValueAsJsonDict("view");
            DateOnly date;
            if (view == null)
            {
                date = JST.Today;
            }
            else
            {
                var dateString = view.GetStringFromPath("state/values/./datepicker-action/selected_date");
                if (dateString == null ||
                    !DateOnly.TryParseExact(dateString, "yyyy-MM-dd", out date))
                {
                    date = JST.Today;
                }
            }
            _ = Slack.UpdateHome(user, date);

            _logger.LogInformation("SlackEvent app_home_opened has been handled successfully.");
            return Ok();
        }

        /// <summary>
        /// Slack インタラクティブコンポーネントのイベント処理を行います。
        /// </summary>
        /// <param name="payload">イベント API の本文です。</param>
        [HttpPost("Interactive")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult PostInteractive([FromForm(Name = "payload")] string payload)
        {
            try
            {
                _logger.LogInformation("Slack interactive received: {payload}", payload);

                var dict = JsonSerializer.Deserialize<JsonDict>(payload);
                if (dict == null)
                {
                    return _logger.Warn(BadRequest("This content cannnot be resolved."));
                }

                // トークン認証
                if (!VerifyToken(dict, out var result))
                {
                    return result;
                }

                // 先頭の action のみを処理する
                var action = dict.GetValueAsJsonDict("actions", 0);
                if (action == null)
                {
                    return _logger.Warn(BadRequest("The content must have an \"actions\" string."));
                }

                var actionId = action.GetValueAsString("action_id");
                return actionId switch
                {
                    "datepicker-action" => DateChanged(dict, action),
                    "button-action" => ButtonClicked(dict, action),
                    _ => _logger.Warn(BadRequest($"Invalid action_id: {actionId}")),
                };
            }
            catch (Exception e)
            {
                _logger.Warn(e);
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = e.ToString(),
                    ContentType = "text/plain",
                };
            }
        }

        /// <summary>
        /// Date Picker の日付が変更された時の処理を行います。
        /// </summary>
        private IActionResult DateChanged(JsonDict payload, JsonDict action)
        {
            // ユーザー ID を取得
            var userId = payload.GetStringFromPath("user/id");
            if (userId == null || !SlackUser.IsValidID(userId))
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }
            var user = new SlackUser(userId);

            // 選択された日付を取得
            var dateString = action.GetValueAsString("selected_date");
            if (dateString == null ||
                !DateOnly.TryParseExact(dateString, "yyyy-MM-dd", out var date))
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }

            // ビューを更新
            _ = Slack.UpdateHome(user, date);

            _logger.LogInformation("Slack Interactive Event has been handled successfully.");
            return Ok();
        }

        /// <summary>
        /// 通知ボタンがクリックされた時の処理を行います。
        /// </summary>
        private IActionResult ButtonClicked(JsonDict payload, JsonDict action)
        {
            // ユーザー ID を取得
            var userId = payload.GetStringFromPath("user/id");
            if (userId == null || !SlackUser.IsValidID(userId))
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }
            var user = new SlackUser(userId);

            // 選択された日付を取得
            var dateString = payload.GetStringFromPath("view/state/values/./datepicker-action/selected_date");
            if (dateString == null ||
                !DateOnly.TryParseExact(dateString, "yyyy-MM-dd", out var date))
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }

            // 選択された時刻を取得
            var hourString = action.GetValueAsString("value");
            if (hourString == null || hourString.Length < "notif_t_".Length ||
                !int.TryParse(hourString.AsSpan("notif_t_".Length), out int hour))
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }

            var dateHour = new DateHour(date, hour);

            // 通知の設定値を取得
            var newValue = hourString["notif_".Length] == 'f';

            // blocks の該当箇所だけ先に変更
            var view = payload.GetValueAsJsonDict("view");
            if (view == null)
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }
            var blocks = view.GetValueAsListOfJsonDict("blocks");
            // accessory/value の値が hourString であるものを探す
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].GetStringFromPath("accessory/value") == hourString)
                {
                    view = view.Replace($"blocks/{i}/accessory/text/text",
                        newValue ? ":no_entry_sign:" : ":bell:");
                    if (newValue)
                    {
                        view = view?.AddJson($"blocks/{i}/accessory", "style", "danger");
                    }
                    else
                    {
                        view = view?.RemoveJson($"blocks/{i}/accessory", "style");
                    }
                    view = view?.Replace($"blocks/{i + 1}/elements/0/text",
                        newValue ? "あなたはこの時間帯への通知をオンにしています。" :
                        "通知はオフになっています。");
                    break;
                }
            }

            if (view == null || !view.ContainsKey("blocks"))
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }
            var newBlock = JsonSerializer.Serialize(view["blocks"]);

            Task.Run(async () =>
            {
                // ビューを先に更新する
                await Slack.UpdateHome(user, newBlock);
                // 設定値をデータベースに格納する
                await Notif.SetUserAsync(dateHour, user, newValue);
                // ビューを更新する
                await Slack.UpdateHome(user, date, GetUserNextSessionId(user));
            });

            _logger.LogInformation("Slack Interactive Event has been handled successfully.");
            return Ok();
        }

        /// <summary>
        /// Slack スラッシュコマンドのイベント処理を行います。
        /// </summary>
        /// <param name="payload">イベント API の本文です。</param>
        [HttpPost("Command")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult PostCommand([FromForm] StringDict payload)
        {
            try
            {
                _logger.LogInformation("Slack interactive received: {payload}",
                    JsonSerializer.Serialize(payload));

                if (payload == null)
                {
                    return _logger.Warn(BadRequest("This content cannnot be resolved."));
                }

                // トークン認証
                if (!VerifyToken(payload, out var result))
                {
                    return result;
                }

                // コマンド名
                if (!payload.TryGetValue("command", out var command))
                {
                    return _logger.Warn(BadRequest("The content must have an \"command\" string."));
                }

                // パラメータ
                if (!payload.TryGetValue("text", out var text))
                {
                    return _logger.Warn(BadRequest("The content must have an \"text\" string."));
                }

                // ユーザー ID
                if (!payload.TryGetValue("user_id", out var userId))
                {
                    return _logger.Warn(BadRequest("The content must have an \"user_id\" string."));
                }
                var user = new SlackUser(userId);

                // レスポンス URL
                if (!payload.TryGetValue("response_url", out var urlStr))
                {
                    return _logger.Warn(BadRequest("The content must have an \"response_url\" string."));
                }
                var url = new Uri(urlStr);

                return command switch
                {
                    "/debug" => ExecuteCommand(new DebugCommand(text, user, url)),
                    "/alert" => ExecuteCommand(new AlertCommand(text, user, url)),
                    "/showalert" => ExecuteCommand(new ShowAlertCommand(text, user, url)),
                    _ => _logger.Warn(BadRequest($"Invalid command: {command}")),
                };
            }
            catch (Exception e)
            {
                _logger.Warn(e);
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = e.ToString(),
                    ContentType = "text/plain",
                };
            }
        }

        /// <summary>
        /// コマンドの処理を実行します。
        /// </summary>
        private IActionResult ExecuteCommand(SlackCommand command)
        {
            command.Execute(out var message);
            return Ok(message);
        }
    }
}
