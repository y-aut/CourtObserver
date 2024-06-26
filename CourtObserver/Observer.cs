﻿using System.Linq;
using System.Text.RegularExpressions;
using COLib;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace CourtObserver
{
    public class Observer : IDisposable
    {
        // 対象のURL
        private const string URL = "https://g-kyoto.pref.kyoto.lg.jp/reserve_j/core_i/init.asp?SBT=1";

        // 待機時間の余裕（最小で 10）
        private const int WAIT_RATE = 30;

        private readonly ChromeDriver driver;
        private ChromeDriver inner;

        /// <summary>
        /// 取得中のコートです。
        /// </summary>
        public Court Court { get; private set; }

        /// <summary>
        /// 取得した空き状況です。
        /// </summary>
        public CourtCalendar CourtCalendar { get; private set; }

        /// <summary>
        /// true にすると取得を終了します。
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// コート状況の更新（新規取得も含む）を検知したときに発生するイベントです。
        /// 現在の画面の取得処理の終わりに発生します。
        /// 引数は、値が更新された日付と、変更後の値をペアにもつ CourtCalendar オブジェクトです。
        /// </summary>
        public event EventHandler<CourtCalendar> CourtStateUpdated = delegate { };

        /// <summary>
        /// コート状況の変更を検知したときに発生するイベントです。
        /// 現在の画面の取得処理の終わりに発生します。
        /// 引数は、値が更新された日付と、変更前の値をペアにもつ CourtCalendar オブジェクトです。
        /// </summary>
        public event EventHandler<CourtCalendar> CourtStateChanged = delegate { };

        public Observer(Court court)
        {
            driver = inner = new ChromeDriver();
            CourtCalendar = new CourtCalendar(court);
            Court = court;
        }

        public void Dispose()
        {
            if (inner != null)
            {
                inner.Dispose();
            }
            driver.Quit();
            driver.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 各施設の番号を取得します。
        /// </summary>
        private static (int, int) GetLocationValue(Court court)
        {
            return court switch
            {
                Court.Takara => (100045, 26103),
                Court.Okazaki => (100047, 26103),
                _ => (0, 0),
            };
        }

        /// <summary>
        /// 各コートについて、「テニスコート」の項目のインデックスを取得します。
        /// </summary>
        private static int GetTennisCourtIndex(Court court)
        {
            return court switch
            {
                Court.Takara or Court.Okazaki => 1,
                _ => 0,
            };
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
        /// <returns>初期化に成功したか。</returns>
        private bool Initialize()
        {
            try
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
                var value = GetLocationValue(Court);
                inner.ExecuteScript(@$"cmdYoyaku_click('{value.Item1}','{value.Item2}');");
                Sleep(200);
                // 2週間表示にする
                driver.FindElement(By.Id("radio1")).Click();
                Sleep(50);
                // アラートを閉じる
                driver.SwitchTo().Alert().Accept();
                Sleep(1000);
                // 「テニスコート」を選択
                var selectShisetu = new SelectElement(driver.FindElement(By.Name("lst_shisetu")));
                selectShisetu.SelectByIndex(GetTennisCourtIndex(Court));
                Sleep(1000);

                return true;
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (NoSuchElementException e)
            {
                Util.WriteInfo("要素が見つかりませんでした。ページを再度初期化します。エラーの詳細:");
                Util.WriteInfo(e.ToString());
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Util.WriteInfo("エラーが発生しました。ページを再度初期化します。エラーの詳細:");
                Util.WriteInfo(e.ToString());
                Console.WriteLine();
            }

            return false;
        }

        /// <summary>
        /// ループしてコート状況を継続的に取得します。
        /// </summary>
        private async Task Loop()
        {
            DateOnly today = JST.Today;
            DateOnly date = today;
            while (true)
            {
                Util.WriteInfo("情報を取得します。");

                // 日付が変わったら古い情報は削除
                if (JST.Today != today)
                {
                    Util.WriteInfo(JST.Now.ToString("MM/dd hh:mm") + ": 日付が変わりました。");

                    CourtCalendar.Clean();
                    await COClient.CleanAsync();
                    today = JST.Today;
                }

                try
                {
                    // 抽選予約、または期間外になるまで取得を続ける
                    while (!UpdateTwoWeeksSince(date))
                    {
                        date = date.AddDays(14);
                    }
                    date = today;
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (NoSuchElementException e)
                {
                    Util.WriteInfo("要素が見つかりませんでした。ページを再度初期化します。エラーの詳細:");
                    Util.WriteInfo(e.ToString());
                    Console.WriteLine();
                    return;
                }
                catch (Exception e)
                {
                    Util.WriteInfo("エラーが発生しました。ページを再度初期化します。エラーの詳細:");
                    Util.WriteInfo(e.ToString());
                    Console.WriteLine();
                    return;
                }
            }
        }

        /// <summary>
        /// 処理を実行します。
        /// </summary>
        public async Task RunAsync()
        {
            await Task.Yield();

            try
            {
                while (true)
                {
                    while (!Initialize()) ;
                    await Loop();
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        /// <summary>
        /// date から 2週間先までの空き状況を更新します。
        /// </summary>
        /// <returns>データの終わりかどうか。</returns>
        private bool UpdateTwoWeeksSince(DateOnly date)
        {
            inner.ExecuteScript(@$"set_cal({date:yyyyMMdd},00000000,99999999)");
            Sleep(1000);
            return UpdateCalendar();
        }

        /// <summary>
        /// 現在表示されている情報をもとに空き状況を更新します。
        /// </summary>
        /// <returns>データの終わりかどうか。</returns>
        private bool UpdateCalendar()
        {
            // 以前に取得していたもので値が変わったもののみ。新しいデータを格納
            CourtCalendar? changed = null;
            // 取得していなかったものも含める。古いデータを格納
            CourtCalendar? updated = null;

            var isEnd = false;
            DateOnly date;
            for (int i = 0; i < 14; i++)
            {
                var header = inner.FindElement(By.Id($"Day_{i}"));
                date = ParseDate(header.Text);
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday &&
                    !CourtCalendar.Holidays.Contains(date))
                {
                    // 背景色を取得
                    var color = header.GetAttribute("bgcolor");
                    if (color == "#ffcccc")
                    {
                        CourtCalendar.Holidays.Add(date);
                    }
                }
                // START_HOUR 時からの各時間を確認
                int time = Const.START_HOUR;
                var td = header.FindElement(By.XPath(@"following-sibling::td"));
                while (true)
                {
                    int hours = int.Parse(td.GetAttribute("colspan")) / 4;
                    // 受付期間外のときは、a タグは存在しない
                    var img = td.FindElements(By.XPath(@"a/img"));
                    if (img.Count == 0)
                    {
                        img = td.FindElements(By.XPath("img"));
                        if (img.Count == 0)
                        {
                            // データが存在しない月なので，for を抜ける
                            isEnd = true;
                            i = 10000;
                            break;
                        }
                    }
                    var state = ParseCourtState(img[0].GetAttribute("alt"));
                    for (int j = 0; j < hours; j++)
                    {
                        // 値が変更されたかを確認する
                        var old = CourtCalendar.GetValue(new DateHour(date, time + j));
                        if (old == null || old != state)
                        {
                            if (updated == null)
                            {
                                updated = new CourtCalendar(Court);
                            }
                            updated.SetValue(new DateHour(date, time + j), state);
                        }
                        if (old != null && old != state)
                        {
                            if (changed == null)
                            {
                                changed = new CourtCalendar(Court);
                            }
                            changed.SetValue(new DateHour(date, time + j), old.Value);
                        }
                        CourtCalendar.SetValue(new DateHour(date, time + j), state);
                    }
                    time += hours;
                    try
                    {
                        td = td.FindElement(By.XPath(@"following-sibling::td"));
                    }
                    catch (NoSuchElementException)
                    {
                        break;
                    }
                }
            }

            if (!isEnd)
            {
                var lastState = CourtCalendar.GetValue(new DateHour(date, 12));
                if (lastState == CourtState.Lottery || lastState == CourtState.OutOfDate)
                {
                    isEnd = true;
                }
            }

            if (updated != null)
            {
                CourtStateUpdated(this, updated);
            }
            if (changed != null)
            {
                CourtStateChanged(this, changed);
            }

            return isEnd;
        }

        /// <summary>
        /// "予約可能" などの文字列を CourtState 型にパースします。
        /// </summary>
        private static CourtState ParseCourtState(string str)
        {
            if (str == "予約可能")
            {
                return CourtState.Empty;
            }
            else if (str == "予約不可")
            {
                return CourtState.Reserved;
            }
            else if (str.Contains("期間外"))
            {
                return CourtState.OutOfDate;
            }
            else if (str.Contains("抽選"))
            {
                return CourtState.Lottery;
            }
            else if (str.Contains('休'))
            {
                return CourtState.Closed;
            }
            else
            {
                return CourtState.Unknown;
            }
        }

        /// <summary>
        /// "10月2日（土）" などの文字列を DateOnly 型にパースします。
        /// </summary>
        private static DateOnly ParseDate(string str)
        {
            var match = Regex.Match(str, @"(\d+)月(\d+)日（(.)）");
            var month = int.Parse(match.Groups[1].Value);
            var day = int.Parse(match.Groups[2].Value);
            var dayOfWeek = (DayOfWeek)Util.DayOfWeekString.IndexOf(match.Groups[3].Value);
            var thisYear = JST.Today.Year;

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
