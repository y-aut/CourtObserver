using System.Drawing.Imaging;

namespace CourtObserver
{
    public static class Program
    {
        // 待機時間の余裕（通常は 20）
        private const int WAIT_RATE = 10;

        private static List<Observer>? observers;
        private static bool flgCancel = false;

        // 監視するコート
        private static readonly Court[] courts = { Court.Takara, Court.Okazaki };

        public static void Main()
        {
            observers = new List<Observer>();
            for (int i = 0; i < courts.Length; i++)
            {
                observers.Add(new Observer(courts[i]));
            }

            Console.WriteLine("取得を開始します。Esc キーで終了します。");
            Console.WriteLine();

            var tasks = new List<Task>();
            for (int i = 0; i < courts.Length; i++)
            {
                var obs = observers[i];
                var court = courts[i];

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
                if (flgCancel) return;
                if (observers == null) continue;

                for (int i = 0; i < observers.Count; i++)
                {
                    using var img = Drawer.DrawCalendar(observers[i].CourtCalendar);
                    img.Save($"image_{i}.png");
                    Slack.SendTextImageAsync(
                        $"{courts[i].ToDisplayString()}の空き状況をお知らせします。:tennis:",
                        $"image_{i}.png").Wait();
                }
            }
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
