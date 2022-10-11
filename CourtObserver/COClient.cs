using COLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CourtObserver
{
    /// <summary>
    /// COServer との通信を行うクライアントです。
    /// </summary>
    public static class COClient
    {
        // COServer の URL
        private const string COSERVER_URL = "https://courtobserver.azurewebsites.net";

        private readonly static HttpClient client = new();

        /// <summary>
        /// 指定した時刻の通知をオンにしているユーザーのリストを返します。
        /// </summary>
        public static async Task<IEnumerable<SlackUser>> GetUsersAsync(DateHour date)
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(
                new HttpMethod("GET"), $"{COSERVER_URL}/Users/{date.ToApiString()}");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.ToString());
            }

            var data = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IEnumerable<SlackUser>>(data);
            if (result == null)
            {
                return new List<SlackUser>();
            }
            return result;
        }

        public static async Task UpdateCourtAsync(DateHour date, Court court, CourtState state)
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                $"{COSERVER_URL}/CourtCalendar/{date.ToApiString()}/{court.ToDataString()}/{state.ToDataString()}");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.ToString());
            }
        }
    }
}
