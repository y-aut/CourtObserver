using COLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace COServer.Controllers
{
    using JsonDict = Dictionary<string, JsonElement>;

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
        /// ユーザー毎のアクセス回数です。
        /// </summary>
        private static readonly Dictionary<string, int> userAccesses = new();

        /// <summary>
        /// 指定したユーザーのアクセス回数を取得します。
        /// </summary>
        public static int GetUserAccess(SlackUser user)
        {
            lock (userAccesses)
            {
                if (!userAccesses.ContainsKey(user.ID))
                {
                    return 0;
                }
                return userAccesses[user.ID];
            }
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
            var user = content.GetValueAsString("user");
            if (user == null || !SlackUser.IsValidID(user))
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }

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
            _ = Slack.UpdateHome(new SlackUser(user), date);

            _logger.LogInformation("SlackEvent app_home_opened has been handled successfully.");
            return Ok();
        }

        /// <summary>
        /// Slack インタラクティブコンポーネントのイベント処理を行います。
        /// </summary>
        /// <param name="payload">イベント API の本文です。</param>
        [HttpPost("Interactive")]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult PostInteractive([FromForm] string payload)
        {
            // urlencoded のときは payload の名前を変更してはならない
            try
            {
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
            var user = payload.GetStringFromPath("user/id");
            if (user == null || !SlackUser.IsValidID(user))
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }

            // 選択された日付を取得
            var dateString = action.GetValueAsString("selected_date");
            if (dateString == null ||
                !DateOnly.TryParseExact(dateString, "yyyy-MM-dd", out var date))
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }

            // ビューを更新
            _ = Slack.UpdateHome(new SlackUser(user), date);

            _logger.LogInformation("Slack Interactive Event has been handled successfully.");
            return Ok();
        }

        /// <summary>
        /// 通知ボタンがクリックされた時の処理を行います。
        /// </summary>
        private IActionResult ButtonClicked(JsonDict payload, JsonDict action)
        {
            // ユーザー ID を取得
            var user = payload.GetStringFromPath("user/id");
            if (user == null || !SlackUser.IsValidID(user))
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }

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

            // 通知の設定を変更して、ビューを更新
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

            int access;
            if (userAccesses.ContainsKey(user))
            {
                access = ++userAccesses[user];
            }
            else
            {
                userAccesses.Add(user, 0);
                access = 0;
            }
            Task.Run(async () =>
            {
                // ビューを先に更新する
                await Slack.UpdateHome(new SlackUser(user), newBlock);

                await Notif.SetUserAsync(new DateHour(date, hour), new SlackUser(user), newValue);
                await Slack.UpdateHome(new SlackUser(user), date, access);
            });

            _logger.LogInformation("Slack Interactive Event has been handled successfully.");
            return Ok();
        }
    }
}
