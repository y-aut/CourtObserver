using COLib;
using Microsoft.AspNetCore.Mvc;

namespace COServer.Controllers
{
    /// <summary>
    /// どの時間の通知をどのユーザーがオンにしているかの情報を扱います。
    /// </summary>
    [ApiController]
    [Route("Users")]
    public class NotifController : ControllerBase
    {
        private readonly ILogger<NotifController> _logger;

        /// <summary>
        /// インスタンスを初期化します。
        /// </summary>
        public NotifController(ILogger<NotifController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストを返します。
        /// </summary>
        /// <param name="date">日付と時刻を表す文字列です。"2022-10-3-16" のように指定します。</param>
        [HttpGet("{date}")]
        public async Task<IActionResult> GetUsersAsync(string date)
        {
            if (!DateHour.TryParse(date, out var dateHour))
            {
                return BadRequest($"{date} is an invalid DateHour string.");
            }
            try
            {
                return Ok(await Notif.GetUsersAsync(dateHour));
            }
            catch (Exception e)
            {
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = e.ToString(),
                    ContentType = "text/plain",
                };
            }
        }

        /// <summary>
        /// 指定した時刻の通知を user がオンにしているかどうかを返します。
        /// </summary>
        /// <param name="date">日付と時刻を表す文字列です。"2022-10-3-16" のように指定します。</param>
        /// <param name="user">Slack ユーザー ID。</param>
        [HttpGet("{date}/{user}")]
        public async Task<IActionResult> GetValueAsync(string date, string user)
        {
            if (!DateHour.TryParse(date, out var dateHour))
            {
                return BadRequest($"{date} is an invalid DateHour string.");
            }
            if (!SlackUser.IsValidID(user))
            {
                return BadRequest($"{user} is an invalid Slack ID string.");
            }
            try
            {
                return Ok(await Notif.GetValueAsync(dateHour, new SlackUser(user)));
            }
            catch (Exception e)
            {
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = e.ToString(),
                    ContentType = "text/plain",
                };
            }
        }

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストに user を追加します。
        /// </summary>
        /// <param name="date">日付と時刻を表す文字列です。"2022-10-3-16" のように指定します。</param>
        /// <param name="user">Slack ユーザー ID。</param>
        /// <returns>データを実際に追加した場合、true が返されます。</returns>
        [HttpPut("{date}/{user}")]
        public async Task<IActionResult> AddUserAsync(string date, string user)
        {
            if (!DateHour.TryParse(date, out var dateHour))
            {
                return BadRequest($"{date} is an invalid DateHour string.");
            }
            if (!SlackUser.IsValidID(user))
            {
                return BadRequest($"{user} is an invalid Slack ID string.");
            }
            try
            {
                return Ok(await Notif.AddUserAsync(dateHour, new SlackUser(user)));
            }
            catch (Exception e)
            {
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = e.ToString(),
                    ContentType = "text/plain",
                };
            }
        }

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストから user を削除します。
        /// </summary>
        /// <param name="date">日付と時刻を表す文字列です。"2022-10-3-16" のように指定します。</param>
        /// <param name="user">Slack ユーザー ID。</param>
        /// <returns>データを実際に削除した場合、true が返されます。</returns>
        [HttpDelete("{date}/{user}")]
        public async Task<IActionResult> RemoveUserAsync(string date, string user)
        {
            if (!DateHour.TryParse(date, out var dateHour))
            {
                return BadRequest($"{date} is an invalid DateHour string.");
            }
            if (!SlackUser.IsValidID(user))
            {
                return BadRequest($"{user} is an invalid Slack ID string.");
            }
            try
            {
                return Ok(await Notif.RemoveUserAsync(dateHour, new SlackUser(user)));
            }
            catch (Exception e)
            {
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = e.ToString(),
                    ContentType = "text/plain",
                };
            }
        }

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストを削除します。
        /// </summary>
        /// <param name="date">日付と時刻を表す文字列です。"2022-10-3-16" のように指定します。</param>
        /// <returns>データを実際に削除した場合、true が返されます。</returns>
        [HttpDelete("{date}")]
        public async Task<IActionResult> RemoveUsersAsync(string date)
        {
            if (!DateHour.TryParse(date, out var dateHour))
            {
                return BadRequest($"{date} is an invalid DateHour string.");
            }
            try
            {
                return Ok(await Notif.RemoveUsersAsync(dateHour));
            }
            catch (Exception e)
            {
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = e.ToString(),
                    ContentType = "text/plain",
                };
            }
        }

        /// <summary>
        /// 昨日までのデータを削除します。
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> CleanAsync()
        {
            try
            {
                await Notif.CleanAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = e.ToString(),
                    ContentType = "text/plain",
                };
            }
        }
    }
}