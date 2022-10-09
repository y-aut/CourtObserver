using COLib;
using Microsoft.AspNetCore.Mvc;

namespace COServer.Controllers
{
    /// <summary>
    /// �ǂ̎��Ԃ̒ʒm���ǂ̃��[�U�[���I���ɂ��Ă��邩�̏��������܂��B
    /// </summary>
    [ApiController]
    [Route("Users")]
    public class NotifController : ControllerBase
    {
        private readonly ILogger<NotifController> _logger;

        /// <summary>
        /// �C���X�^���X�����������܂��B
        /// </summary>
        public NotifController(ILogger<NotifController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// �w�肵�������̒ʒm���I���ɂ��Ă��郆�[�U�[�̃��X�g��Ԃ��܂��B
        /// </summary>
        /// <param name="date">���t�Ǝ�����\��������ł��B"2022-10-3-16" �̂悤�Ɏw�肵�܂��B</param>
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
        /// �w�肵�������̒ʒm�� user ���I���ɂ��Ă��邩�ǂ�����Ԃ��܂��B
        /// </summary>
        /// <param name="date">���t�Ǝ�����\��������ł��B"2022-10-3-16" �̂悤�Ɏw�肵�܂��B</param>
        /// <param name="user">Slack ���[�U�[ ID�B</param>
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
        /// �w�肵�������̒ʒm���I���ɂ��Ă��郆�[�U�[�̃��X�g�� user ��ǉ����܂��B
        /// </summary>
        /// <param name="date">���t�Ǝ�����\��������ł��B"2022-10-3-16" �̂悤�Ɏw�肵�܂��B</param>
        /// <param name="user">Slack ���[�U�[ ID�B</param>
        /// <returns>�f�[�^�����ۂɒǉ������ꍇ�Atrue ���Ԃ���܂��B</returns>
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
        /// �w�肵�������̒ʒm���I���ɂ��Ă��郆�[�U�[�̃��X�g���� user ���폜���܂��B
        /// </summary>
        /// <param name="date">���t�Ǝ�����\��������ł��B"2022-10-3-16" �̂悤�Ɏw�肵�܂��B</param>
        /// <param name="user">Slack ���[�U�[ ID�B</param>
        /// <returns>�f�[�^�����ۂɍ폜�����ꍇ�Atrue ���Ԃ���܂��B</returns>
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
        /// �w�肵�������̒ʒm���I���ɂ��Ă��郆�[�U�[�̃��X�g���폜���܂��B
        /// </summary>
        /// <param name="date">���t�Ǝ�����\��������ł��B"2022-10-3-16" �̂悤�Ɏw�肵�܂��B</param>
        /// <returns>�f�[�^�����ۂɍ폜�����ꍇ�Atrue ���Ԃ���܂��B</returns>
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
        /// ����܂ł̃f�[�^���폜���܂��B
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