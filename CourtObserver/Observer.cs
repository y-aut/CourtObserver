using System.Linq;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace CourtObserver
{
    public enum Court
    {
        Takara,
        Okazaki,
    }

    public class Observer : IDisposable
    {
        // 対象のURL
        private const string URL = "https://g-kyoto.pref.kyoto.lg.jp/reserve_j/core_i/init.asp?SBT=1";

        // 待機時間の余裕（最小で 10）
        private const int WAIT_RATE = 10;

        private readonly ChromeDriver driver;
        private ChromeDriver inner;

        /// <summary>
        /// 取得した空き状況です。
        /// </summary>
        public CourtCalendar CourtCalendar { get; private set; }

        /// <summary>
        /// true にすると取得を終了します。
        /// </summary>
        public bool Cancel { get; set; }

        public Observer()
        {
            driver = inner = new ChromeDriver();
            CourtCalendar = new CourtCalendar();
        }

        public void Dispose()
        {
            if (inner != null)
            {
                inner.Dispose();
            }
            driver.Quit();
            driver.Dispose();
        }

        /// <summary>
        /// 各施設の番号を取得します。
        /// </summary>
        private static (int, int) GetLocationValue(Court court)
        {
            switch (court)
            {
                case Court.Takara:
                    return (100045, 26103);
                case Court.Okazaki:
                    return (100047, 26103);
                default:
                    return (0, 0);
            }
        }

        /// <summary>
        /// 各コートについて、「テニスコート」の項目のインデックスを取得します。
        /// </summary>
        private static int GetTennisCourtIndex(Court court)
        {
            switch (court)
            {
                case Court.Takara:
                case Court.Okazaki:
                    return 1;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// ms * WAIT_RATE ミリ秒だけ処理を停止します。
        /// </summary>
        private void Sleep(int ms)
        {
            // キャンセルフラグを確認する間隔
            const int INTERVAL = 100;

            for (int i = 0; i < ms * WAIT_RATE / INTERVAL; i++)
            {
                Thread.Sleep(INTERVAL);
                if (Cancel)
                {
                    throw new TaskCanceledException();
                }
            }
        }

        /// <summary>
        /// 指定したコートの今後2週間の情報を表示するページまで遷移します。
        /// </summary>
        public void Initialize(Court court)
        {
            // URLへ遷移
            driver.Navigate().GoToUrl(URL);
            Sleep(200);

            var frame = driver.FindElement(By.Name("MainFrame"));
            inner = (ChromeDriver)driver.SwitchTo().Frame(frame);

            inner.ExecuteScript(@"cmdMokuteki_click('100040', 'テニス');");
            Sleep(200);
            inner.ExecuteScript(@"cmdSelect_click('00000', '京都市');");
            Sleep(50);
            // 検索ボタンをクリック
            driver.FindElement(By.Name("btn_next")).Click();
            Sleep(200);
            // 施設を選択
            var value = GetLocationValue(court);
            inner.ExecuteScript(@$"cmdYoyaku_click('{value.Item1}','{value.Item2}');");
            Sleep(200);
            // 2週間表示にする
            driver.FindElement(By.Id("radio1")).Click();
            Sleep(10);
            // アラートを閉じる
            driver.SwitchTo().Alert().Accept();
            Sleep(1000);
            // 「テニスコート」を選択
            var selectShisetu = new SelectElement(driver.FindElement(By.Name("lst_shisetu")));
            selectShisetu.SelectByIndex(GetTennisCourtIndex(court));
            Sleep(1000);
            UpdateCalendar();
        }

        /// <summary>
        /// ループしてコート状況を継続的に取得します。
        /// </summary>
        public void Loop()
        {
            // 最初の 2週間は取得済み
            DateTime today = DateTime.Today;
            DateOnly date = DateOnly.FromDateTime(today);
            while (true)
            {
                // 日付が変わったら古い情報は削除
                if (DateTime.Today != today)
                {
                    CourtCalendar.Clean();
                    today = DateTime.Today;
                }

                date = date.AddDays(14);
                UpdateTwoWeeksSince(date);
                date = date.AddDays(14);
                UpdateTwoWeeksSince(date);
                date = DateOnly.FromDateTime(today);
                UpdateTwoWeeksSince(date);

                Sleep(15000);
            }
        }

        /// <summary>
        /// date から 2週間先までの空き状況を更新します。
        /// </summary>
        private void UpdateTwoWeeksSince(DateOnly date)
        {
            inner.ExecuteScript(@$"set_cal({date:yyyyMMdd},00000000,99999999)");
            Sleep(1000);
            UpdateCalendar();
        }

        /// <summary>
        /// 現在表示されている情報をもとに空き状況を更新します。
        /// </summary>
        private void UpdateCalendar()
        {
            for (int i = 0; i < 14; i++)
            {
                var header = inner.FindElement(By.Id($"Day_{i}"));
                var date = ParseDate(header.Text);
                // 8 時からの各時間を確認
                int time = 8;
                var td = header.FindElement(By.XPath(@"following-sibling::td"));
                while (true)
                {
                    int hours = int.Parse(td.GetAttribute("colspan")) / 4;
                    var available = td.FindElement(By.XPath(@"a/img")).GetAttribute("alt") == "予約可能";
                    for (int j = 0; j < hours; j++)
                    {
                        CourtCalendar.SetValue(date, time + j, available);
                    }
                    time += hours;
                    try
                    {
                        td = td.FindElement(By.XPath(@"following-sibling::td"));
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// "10月2日（土）" などの文字列を DateOnly 型にパースします。
        /// </summary>
        private static DateOnly ParseDate(string str)
        {
            var dayOfWeekString = new List<string>() { "日", "月", "火", "水", "木", "金", "土" };

            var match = Regex.Match(str, @"(\d+)月(\d+)日（(.)）");
            var month = int.Parse(match.Groups[1].Value);
            var day = int.Parse(match.Groups[2].Value);
            var dayOfWeek = (DayOfWeek)dayOfWeekString.IndexOf(match.Groups[3].Value);
            var thisYear = DateTime.Today.Year;

            // 2/29 なら閏年の年を採用
            if (month == 2 && day == 29)
            {
                for (int i = -1; ; i++)
                {
                    if (DateTime.IsLeapYear(thisYear + i))
                    {
                        return new DateOnly(thisYear + i, month, day);
                    }
                }
            }

            // 曜日が一致する年を採用
            for (int i = -1; ; i++)
            {
                if (new DateOnly(thisYear + i, month, day).DayOfWeek == dayOfWeek)
                {
                    return new DateOnly(thisYear + i, month, day);
                }
            }
        }
    }
}
