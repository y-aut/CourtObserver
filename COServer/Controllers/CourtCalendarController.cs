using COLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace COServer.Controllers
{
    /// <summary>
    /// コート空き状況を取得・設定する API を実装します。
    /// </summary>
    [Route("[controller]")]
    [ApiController]
    public class CourtCalendarController : ControllerBase
    {
        private readonly ILogger<CourtCalendarController> _logger;

        /// <summary>
        /// インスタンスを初期化します。
        /// </summary>
        public CourtCalendarController(ILogger<CourtCalendarController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 指定した日のコートの空き状況のリストを (court, state) で更新します。
        /// </summary>
        /// <param name="date">日付と時刻を表す文字列です。"2022-10-3-16" のように指定します。</param>
        /// <param name="court">コートのローマ字表記です。(例) "takara", "okazaki"</param>
        /// <param name="state">空き状況を表す文字列です。(例) "empty", "reserved"</param>
        /// <returns>データを実際に追加した場合、true が返されます。</returns>
        [HttpPost("{date}/{court}/{state}")]
        public async Task<IActionResult> UpdateCourtAsync(string date, string court, string state)
        {
            if (!DateHour.TryParse(date, out var dateHour))
            {
                return _logger.Warn(BadRequest($"{date} is an invalid DateHour string."));
            }
            if (!CourtValue.TryParse($"{court},{state}", out var courtVal))
            {
                return _logger.Warn(BadRequest($"{court},{state} is an invalid CourtValue string."));
            }
            try
            {
                return Ok(await CourtCalendarIO.UpdateCourtAsync(dateHour, courtVal));
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
        /// 昨日までのデータを削除します。
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> CleanAsync()
        {
            try
            {
                await CourtCalendarIO.CleanAsync();
                return Ok();
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
    }
}
