using System.Drawing;

namespace CourtObserver
{
    /// <summary>
    /// コート空き状況を画像に変換するためのクラスです。
    /// </summary>
    public static class Drawer
    {
        // 定数
        private const int CELL_WIDTH = 60;
        private const int CELL_HEIGHT = 60;
        private const int ROW_HEADER_WIDTH = 300;
        private const int COL_HEADER_HEIGHT = CELL_HEIGHT;

        /// <summary>
        /// 抽選予約、または期間外になるまでのコートの空き状況を画像に変換します。
        /// </summary>
        public static Image DrawCalendar(CourtCalendar calendar)
        {
            // 合計日数
            int days;
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            for (int i = 0; ; i++)
            {
                var state = calendar.GetValue(today.AddDays(i), 12);
                if (state == null || state == CourtState.Lottery || state == CourtState.OutOfDate)
                {
                    days = i;
                    break;
                }
            }

            var bmp = new Bitmap(
                ROW_HEADER_WIDTH + CELL_WIDTH * Observer.HOURS_COUNT,
                COL_HEADER_HEIGHT + CELL_HEIGHT * days);
            using Graphics g = Graphics.FromImage(bmp);

            // 背景色
            g.Clear(Color.White);
            for (int i = 0; i < days; i += 2)
            {
                g.FillRectangle(Brushes.LightSkyBlue, new Rectangle(
                    0, COL_HEADER_HEIGHT + CELL_HEIGHT * i, bmp.Width, CELL_HEIGHT));
            }

            // 罫線
            var pen = new Pen(Color.SkyBlue, 3);
            for (int i = 0; i < days; i++)
            {
                g.DrawLine(pen, 0, COL_HEADER_HEIGHT + CELL_HEIGHT * i,
                    bmp.Width, COL_HEADER_HEIGHT + CELL_HEIGHT * i);
            }
            for (int i = 0; i < Observer.HOURS_COUNT; i++)
            {
                g.DrawLine(pen, ROW_HEADER_WIDTH + CELL_WIDTH * i, 0,
                    ROW_HEADER_WIDTH + CELL_WIDTH * i, bmp.Height);
            }

            // ヘッダの文字
            var font = new Font("MS ゴシック", 30);
            var format = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };

            for (int i = 0; i < days; i++)
            {
                g.DrawString(today.AddDays(i).ToDisplayString(), font, Brushes.Black, GetRowHeader(i), format);
            }
            for (int i = 0; i < Observer.HOURS_COUNT; i++)
            {
                g.DrawString((Observer.START_HOUR + i).ToString(), font, Brushes.Black, GetColHeader(i), format);
            }

            // 空き状況

            // 当日のみ 1時間おき
            for (int hour = Observer.START_HOUR; hour < Observer.END_HOUR; hour++)
            {
                DrawCourtState(g, GetCell(0, hour, 1), calendar.GetValue(today, hour));
            }

            for (int i = 1; i < days; i++)
            {
                for (int hour = Observer.START_HOUR; hour < Observer.END_HOUR - 2; hour += 2)
                {
                    DrawCourtState(g, GetCell(i, hour, hour == 18 ? 3 : 2), calendar.GetValue(today.AddDays(i), hour));
                }
            }

            return bmp;
        }

        /// <summary>
        /// 日付と時刻からセルを取得します。
        /// </summary>
        /// <param name="dayOffset">初日を 0 とする相対日数。</param>
        /// <param name="hour">始まりの時刻。</param>
        /// <param name="span">結合するセル数。</param>
        private static Rectangle GetCell(int dayOffset, int hour, int span)
        {
            return new Rectangle(
                ROW_HEADER_WIDTH + CELL_WIDTH * (hour - Observer.START_HOUR),
                COL_HEADER_HEIGHT + CELL_HEIGHT * dayOffset,
                CELL_WIDTH * span, CELL_HEIGHT);
        }

        /// <summary>
        /// 日付から行ヘッダを取得します。
        /// </summary>
        /// <param name="dayOffset">初日を 0 とする相対日数。</param>
        private static Rectangle GetRowHeader(int dayOffset)
        {
            return new Rectangle(
                0, COL_HEADER_HEIGHT + CELL_HEIGHT * dayOffset,
                ROW_HEADER_WIDTH, CELL_HEIGHT);
        }

        /// <summary>
        /// 時刻から列ヘッダを取得します。
        /// </summary>
        /// <param name="hour">始まりの時刻。</param>
        private static Rectangle GetColHeader(int hour)
        {
            return new Rectangle(
                ROW_HEADER_WIDTH + CELL_WIDTH * hour, 0,
                CELL_WIDTH, COL_HEADER_HEIGHT);
        }

        /// <summary>
        /// コートの空き状況を、指定した領域に描画します。
        /// </summary>
        private static void DrawCourtState(Graphics g, Rectangle rect, CourtState? state)
        {
            if (state == null)
            {
                return;
            }

            Image? img = state switch
            {
                CourtState.Empty => Resource.empty,
                CourtState.Reserved => Resource.reserved,
                CourtState.Lottery => Resource.lottery,
                CourtState.OutOfDate => Resource.outofdate,
                CourtState.Closed => Resource.closed,
                _ => null
            };

            if (img == null)
            {
                return;
            }

            // rect の短辺の 90% を一辺とする正方形に描画する
            int side = (int)(Math.Min(rect.Width, rect.Height) * 0.9);
            var newRect = new Rectangle(
                rect.X + (rect.Width - side) / 2,
                rect.Y + (rect.Height - side) / 2,
                side, side);
            g.DrawImage(img, newRect);
        }
    }
}
