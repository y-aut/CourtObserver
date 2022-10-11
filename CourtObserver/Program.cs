using COLib;

namespace CourtObserver
{
    public static class Program
    {
        // 待機時間の余裕（通常は 20）
        private const int WAIT_RATE = 20;

        private static readonly List<Observer> observers = new();

        // 情報が最後にアップロードしてから更新されたかどうか
        private static readonly List<bool> updated = new();

        private static bool flgCancel = false;

        // 監視するコート
        private static readonly Court[] courts = { Court.Takara, Court.Okazaki };

        public static void Main()
        {
            for (int i = 0; i < courts.Length; i++)
            {
                observers.Add(new Observer(courts[i]));
                updated.Add(false);
            }

            Util.WriteInfo("取得を開始します。Esc キーで終了します。");
            Console.WriteLine();

            var tasks = new List<Task>();
            for (int i = 0; i < courts.Length; i++)
            {
                var obs = observers[i];
                var court = courts[i];

                // 変更を検知する
                obs.CourtStateUpdated += CourtState_Updated;
                obs.CourtStateChanged += CourtState_Changed;

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        obs.Initialize();
                        obs.Loop();
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }));
            }

            // コート状況をアップロード
            tasks.Add(UploadCalendar());

            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    Util.WriteInfo("アプリケーションを終了します。");
                    Console.WriteLine();
                    flgCancel = true;
                    break;
                }
            }

            observers.ForEach(i => i.Cancel = true);
            Task.WaitAll(tasks.ToArray());

            observers.ForEach(i => i.Dispose());
        }

        /// <summary>
        /// 定期的に取得したコート状況をアップロードします。
        /// </summary>
        private static async Task UploadCalendar()
        {
            try
            {
                // 次回のアップロード時刻
                List<DateTime> next = new();
                var now = JST.Now;

                for (int i = 0; i < observers.Count; i++)
                {
                    var minute = (60 / observers.Count) * i;
                    next.Add(new DateTime(now.Year, now.Month, now.Day, now.Hour, minute, 0));
                    if (now.Minute >= minute)
                    {
                        next[i] = next[i].AddHours(1);
                    }
                }

                while (!flgCancel)
                {
                    Sleep(3000);
                    if (flgCancel)
                    {
                        break;
                    }

                    now = JST.Now;
                    for (int i = 0; i < observers.Count; i++)
                    {
                        // アップロード時刻かどうか
                        if (now < next[i])
                        {
                            continue;
                        }
                        // 1時間に 1回アップロードする
                        next[i] = next[i].AddHours(1);

                        // 最後にアップロードしたときから変更があれば再アップロード
                        if (!updated[i])
                        {
                            Util.WriteInfo("アップロード時刻になりましたが、変更がないためスキップされました。");
                            continue;
                        }

                        string fileName = $"image_{courts[i].ToDataString()}.png";
                        using var img = Drawer.DrawCalendar(observers[i].CourtCalendar);
                        img.Save(fileName);
                        await Slack.SendTextImageAsync(
                            $"{courts[i].ToDisplayString()}の空き状況をお知らせします。:tennis:",
                            fileName);

                        updated[i] = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Util.WriteInfo(ex.ToString());
            }
        }

        /// <summary>
        /// 情報の更新をハンドルします。
        /// </summary>
        private static async void CourtState_Updated(object? sender, CourtCalendar e)
        {
            // API に新しいデータを送る
            try
            {
                Util.WriteInfo($"データが更新されました: {e.Court.ToDisplayString()}, " +
                    $"{e.GetDates().First().ToApiString()}" +
                    (e.GetDates().Count == 1 ? "" : $" 他 {e.GetDates().Count - 1} コマ"));

                for (int i = 0; i < courts.Length; i++)
                {
                    if (courts[i] == e.Court)
                    {
                        updated[i] = true;
                        break;
                    }
                }

                foreach (var date in e.GetDates())
                {
                    var val = e.GetValue(date);
                    if (val == null)
                    {
                        continue;
                    }
                    await COClient.UpdateCourtAsync(date, e.Court, val.Value);
                    Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Util.WriteInfo(ex.ToString());
            }
        }

        /// <summary>
        /// コート状況の変更をハンドルします。
        /// </summary>
        private static async void CourtState_Changed(object? sender, CourtCalendar e)
        {
            Util.WriteInfo($"コートの空き状況が変更されました: {e.Court.ToDisplayString()}, " +
                $"{e.GetDates().First().ToApiString()}" +
                (e.GetDates().Count == 1 ? "" : $" 他 {e.GetDates().Count - 1} コマ"));

            Observer? obs = null;

            for (int i = 0; i < courts.Length; i++)
            {
                if (courts[i] == e.Court)
                {
                    obs = observers[i];
                    break;
                }
            }

            if (obs == null)
            {
                // ありえない
                return;
            }

            var tasks = new List<Task>();

            foreach (var date in e.GetDates())
            {
                var oldState = e.GetValue(date);
                var newState = obs.CourtCalendar.GetValue(date);
                if (oldState != CourtState.Empty && newState == CourtState.Empty)
                {
                    // この時間帯の通知をオンにしている人がいれば、メンションしてメッセージを送信
                    try
                    {
                        var users = await COClient.GetUsersAsync(date);
                        if (users.Any())
                        {
                            tasks.Add(Slack.SendTextAsync($":bell: {e.Court.ToDisplayString()}\n" +
                                $"*{date.Date.ToDisplayString()}{date.Hour}時* からのコートが予約できるようになりました。",
                                users));
                        }
                    }
                    catch (Exception ex)
                    {
                        Util.WriteInfo(ex.ToString());
                    }
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// キャンセルフラグを確認しながら、ms * WAIT_RATE ミリ秒だけ処理を停止します。
        /// </summary>
        private static void Sleep(int ms)
        {
            // キャンセルフラグを確認する間隔
            const int INTERVAL = 100;

            for (int i = 0; i < ms * WAIT_RATE / INTERVAL; i++)
            {
                Thread.Sleep(INTERVAL);
                if (flgCancel)
                {
                    return;
                }
            }
        }
    }
}
