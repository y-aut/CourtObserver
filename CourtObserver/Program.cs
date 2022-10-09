namespace CourtObserver
{
    public static class Program
    {
        // 待機時間の余裕（通常は 20）
        private const int WAIT_RATE = 10;

        private static readonly List<Observer> observers = new();

        // 情報が最後にアップロードしてから更新されたかどうか
        private static readonly List<bool> changed = new();

        private static bool flgCancel = false;

        // 監視するコート
        private static readonly Court[] courts = { Court.Takara, Court.Okazaki };

        public static void Main()
        {
            for (int i = 0; i < courts.Length; i++)
            {
                observers.Add(new Observer(courts[i]));
                changed.Add(false);
            }

            Console.WriteLine("取得を開始します。Esc キーで終了します。");
            Console.WriteLine();

            var tasks = new List<Task>();
            for (int i = 0; i < courts.Length; i++)
            {
                var obs = observers[i];
                var court = courts[i];

                // 変更を検知する
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
            tasks.Add(Task.Run(UploadCalendar));

            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("アプリケーションを終了します。");
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
        private static void UploadCalendar()
        {
            while (!flgCancel)
            {
                Sleep(15000);
                if (flgCancel)
                {
                    break;
                }

                for (int i = 0; i < observers.Count; i++)
                {
                    // 最後にアップロードしたときから変更があれば再アップロード
                    if (!changed[i])
                    {
                        continue;
                    }

                    using var img = Drawer.DrawCalendar(observers[i].CourtCalendar);
                    img.Save($"image_{i}.png");
                    Slack.SendTextImageAsync(
                        $"{courts[i].ToDisplayString()}の空き状況をお知らせします。:tennis:",
                        $"image_{i}.png").Wait();

                    changed[i] = false;
                }
            }
        }

        /// <summary>
        /// コート状況の変更をハンドルします。
        /// </summary>
        private static async void CourtState_Changed(object? sender, CourtCalendar e)
        {
            Observer? obs = null;

            for (int i = 0; i < courts.Length; i++)
            {
                if (courts[i] == e.Court)
                {
                    changed[i] = true;
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
                        Console.WriteLine(ex.ToString());
                    }
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// キャンセルフラグを確認しながら、ms ミリ秒だけ処理を停止します。
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
