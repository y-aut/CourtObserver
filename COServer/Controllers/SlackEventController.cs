using COLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult PostEvent(JsonDict? payload)
        {
            try
            {
                if (payload == null)
                {
                    return _logger.Warn(BadRequest("This content cannnot be resolved."));
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
        /// URL 認証を行います。
        /// </summary>
        private IActionResult VerifyUrl(JsonDict content)
        {
            var token = content.GetValueAsString("token");
            var challenge = content.GetValueAsString("challenge");
            if (token == null || challenge == null)
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }

            if (token != VERIFICATION_TOKEN)
            {
                return _logger.Warn(Unauthorized("Invalid token."));
            }

            return Ok(challenge);
        }

        /// <summary>
        /// イベントを処理します。
        /// </summary>
        private IActionResult HandleEvent(JsonDict content)
        {
            var token = content.GetValueAsString("token");
            if (token == null)
            {
                return _logger.Warn(BadRequest("Illegal content."));
            }

            if (token != VERIFICATION_TOKEN)
            {
                return _logger.Warn(Unauthorized("Invalid token."));
            }

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

            _ = Slack.UpdateHome(new SlackUser(user), content.GetValueAsJsonDict("view"));

            _logger.LogInformation("SlackEvent app_home_opened has been handled successfully.");
            return Ok();
        }
    }
}
