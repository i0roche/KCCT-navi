using System.Drawing.Drawing2D;

using team1.Controls.Modern;
using team1.Core.Models;

namespace team1.Controls
{
    /// <summary>
    /// 地図の部屋をクリックしたときに、その場所の近くに出る吹き出し (6班の PopUpArea 相当)。
    /// 名前・建物・階と、「ここから出発」「ここへ行く」「お気に入り」を出す。
    /// </summary>
    internal sealed class PlacePopup : Control
    {
        public event Action<Place>? SetAsStart;
        public event Action<Place>? SetAsGoal;
        public event Action<Place>? ToggleFavorite;

        private readonly ModernButton _start;
        private readonly ModernButton _goal;
        private readonly ModernButton _favorite;
        private string _description = "";

        public Place? Place { get; private set; }

        public PlacePopup()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            float dpi = Theme.Scale(this);
            Visible = false;
            BackColor = Theme.Surface;
            Font = Theme.Ui(10f);
            Size = new Size((int)(300 * dpi), (int)(176 * dpi));

            int bottom = (int)(176 * dpi) - (int)(14 * dpi) - (int)(34 * dpi);
            _start = new ModernButton { Text = "ここから出発", Left = (int)(14 * dpi), Top = bottom, Width = (int)(120 * dpi), Height = (int)(34 * dpi), Font = Theme.Ui(10f) };
            _start.Click += (_, _) => { if (Place != null) SetAsStart?.Invoke(Place); };
            _goal = new ModernButton { Text = "ここへ行く", Primary = true, Left = (int)(140 * dpi), Top = bottom, Width = (int)(104 * dpi), Height = (int)(34 * dpi), Font = Theme.Ui(10f) };
            _goal.Click += (_, _) => { if (Place != null) SetAsGoal?.Invoke(Place); };
            _favorite = new ModernButton { Icon = IconKind.Star, Left = (int)(250 * dpi), Top = bottom, Width = (int)(36 * dpi), Height = (int)(34 * dpi) };
            _favorite.Click += (_, _) => { if (Place != null) ToggleFavorite?.Invoke(Place); };

            Controls.AddRange([_start, _goal, _favorite]);
            // 背景はボタンの角の外側の色 (カードの白) にそろえる
        }

        /// <summary>場所を表示する。favoritesEnabled が false なら (展示中など) お気に入りの欄を出さない</summary>
        public void ShowFor(Place place, Point at, bool favoritesEnabled, bool isFavorite)
        {
            Place = place;
            _description = place.Description ?? "";
            _favorite.Visible = favoritesEnabled && place.Id.StartsWith("room_");
            _favorite.Selected = isFavorite;
            MoveTo(at);
            Visible = true;
            BringToFront();
            Invalidate();
        }

        /// <summary>吹き出しを at (場所の画面上の位置) の右上に置く。親の端からはみ出さないようにする</summary>
        public void MoveTo(Point at)
        {
            if (Parent == null) return;
            int x = Math.Min(at.X + 14, Math.Max(0, Parent.ClientSize.Width - Width - 4));
            int y = Math.Min(Math.Max(at.Y - Height - 8, 4), Math.Max(0, Parent.ClientSize.Height - Height - 4));
            Location = new Point(Math.Max(x, 0), y);
        }

        public void HidePopup()
        {
            Visible = false;
            Place = null;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            // 角を丸くする (縁は OnPaint で細い線を引いてなじませる)
            using var path = Theme.RoundedRect(new RectangleF(0, 0, Width, Height), 12f);
            Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float dpi = Theme.Scale(this);

            using (var back = new SolidBrush(Theme.Surface)) g.FillRectangle(back, ClientRectangle);
            using (var path = Theme.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 12f))
            using (var pen = new Pen(Theme.Border)) g.DrawPath(pen, path);

            if (Place == null) return;

            int left = (int)(16 * dpi);
            int width = Width - left * 2;
            TextRenderer.DrawText(g, Place.Name, Theme.Ui(12f, FontStyle.Bold), new Rectangle(left, (int)(12 * dpi), width, (int)(28 * dpi)), Theme.Text,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, $"{Place.Building}  {Place.Floor}F", Font, new Rectangle(left, (int)(42 * dpi), width, (int)(22 * dpi)), Theme.Muted,
                TextFormatFlags.NoPadding);
            if (_description.Length > 0)
            {
                TextRenderer.DrawText(g, _description, Font, new Rectangle(left, (int)(68 * dpi), width, (int)(40 * dpi)), Theme.Text,
                    TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }
    }
}
