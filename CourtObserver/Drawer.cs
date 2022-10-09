using COLib;
using System.Drawing;

namespace CourtObserver
{
    /// <summary>
    /// コート空き状況を画像に変換するためのクラスです。
    /// </summary>
    public static class Drawer
    {
        // 定数
        private const int CELL_WIDTH = 80;
        private const int CELL_HEIGHT = 60;
        private const int ROW_HEADER_WIDTH = 310;
        private const int COL_HEADER_HEIGHT = CELL_HEIGHT;
        private const int TOP_HEADER_HEIGHT = CELL_HEIGHT;

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
                var state = calendar.GetValue(new DateHour(today.AddDays(i), 12));
                if (state == null || state == CourtState.Lottery || state == CourtState.OutOfDate)
                {
                    days = i;
                    break;
                }
            }

            var bmp = new Bitmap(GetCellX(Observer.END_HOUR), GetCellY(days));
            using Graphics g = Graphics.FromImage(bmp);

            // 背景色
            g.Clear(Color.LemonChiffon);
            // ヘッダ
            g.FillRectangle(Brushes.MediumSeaGreen, new Rectangle(0, 0, bmp.Width, TOP_HEADER_HEIGHT));
            for (int i = 0; i < days; i++)
            {
                var date = today.AddDays(i);

                Brush? brush;
                if (date.DayOfWeek == DayOfWeek.Saturday)
                {
                    brush = Brushes.PaleTurquoise;
                }
                else if (date.DayOfWeek == DayOfWeek.Sunday || CourtCalendar.Holidays.Contains(date))
                {
                    brush = Brushes.Pink;
                }
                else
                {
                    brush = null;
                }

                if (brush != null)
                {
                    g.FillRectangle(brush, new Rectangle(0, GetCellY(i), bmp.Width, CELL_HEIGHT));
                }
            }

            // 罫線
            var pen = new Pen(Color.Plum, 3);
            // 横
            for (int i = 0; i < days; i++)
            {
                g.DrawLine(pen, 0, GetCellY(i), bmp.Width, GetCellY(i));
            }
            // 縦
            int[] primaryHour = { 8, 10, 12, 14, 16, 18 };
            for (int i = Observer.START_HOUR; i < Observer.END_HOUR; i++)
            {
                if (primaryHour.Contains(i))
                {
                    // 下まで引く
                    g.DrawLine(pen, GetCellX(i), TOP_HEADER_HEIGHT, GetCellX(i), bmp.Height);
                }
                else
                {
                    // 初日まで引く
                    g.DrawLine(pen, GetCellX(i), TOP_HEADER_HEIGHT, GetCellX(i), GetCellY(1));
                }
            }

            // ヘッダの文字
            var font = new Font("MS ゴシック", 30);
            var format = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };

            g.DrawString(calendar.Court.ToDisplayString(), font, Brushes.White,
                new Rectangle(0, 0, bmp.Width, TOP_HEADER_HEIGHT), format);
            for (int i = 0; i < days; i++)
            {
                g.DrawString(today.AddDays(i).ToDisplayString(), font, Brushes.Black, GetRowHeader(i), format);
            }
            for (int i = Observer.START_HOUR; i < Observer.END_HOUR; i++)
            {
                g.DrawString(i.ToString(), font, Brushes.Black, GetColHeader(i), format);
            }

            // 空き状況

            // 当日のみ 1時間おき
            for (int hour = Observer.START_HOUR; hour < Observer.END_HOUR; hour++)
            {
                DrawCourtState(g, GetCell(0, hour, 1), calendar.GetValue(new DateHour(today, hour)));
            }

            for (int i = 1; i < days; i++)
            {
                for (int hour = Observer.START_HOUR; hour < Observer.END_HOUR - 2; hour += 2)
                {
                    DrawCourtState(g, GetCell(i, hour, hour == 18 ? 3 : 2), calendar.GetValue(new DateHour(today.AddDays(i), hour)));
                }
            }

            return bmp;
        }

        /// <summary>
        /// セルの X 座標を取得します。
        /// </summary>
        /// <param name="hour">始まりの時刻。</param>
        private static int GetCellX(int hour)
        {
            return ROW_HEADER_WIDTH + CELL_WIDTH * (hour - Observer.START_HOUR);
        }

        /// <summary>
        /// セルの Y 座標を取得します。
        /// </summary>
        /// <param name="dayOffset">初日を 0 とする相対日数。</param>
        private static int GetCellY(int dayOffset)
        {
            return TOP_HEADER_HEIGHT + COL_HEADER_HEIGHT + CELL_HEIGHT * dayOffset;
        }

        /// <summary>
        /// 日付と時刻からセルを取得します。
        /// </summary>
        /// <param name="dayOffset">初日を 0 とする相対日数。</param>
        /// <param name="hour">始まりの時刻。</param>
        /// <param name="span">結合するセル数。</param>
        private static Rectangle GetCell(int dayOffset, int hour, int span)
        {
            return new Rectangle(GetCellX(hour), GetCellY(dayOffset),
                CELL_WIDTH * span, CELL_HEIGHT);
        }

        /// <summary>
        /// 日付から行ヘッダを取得します。
        /// </summary>
        /// <param name="dayOffset">初日を 0 とする相対日数。</param>
        private static Rectangle GetRowHeader(int dayOffset)
        {
            return new Rectangle(0, GetCellY(dayOffset), ROW_HEADER_WIDTH, CELL_HEIGHT);
        }

        /// <summary>
        /// 時刻から列ヘッダを取得します。
        /// </summary>
        /// <param name="hour">始まりの時刻。</param>
        private static Rectangle GetColHeader(int hour)
        {
            return new Rectangle(GetCellX(hour), TOP_HEADER_HEIGHT, CELL_WIDTH, COL_HEADER_HEIGHT);
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
