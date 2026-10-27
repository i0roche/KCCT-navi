namespace team1.Controls.Modern
{
    /// <summary>メニューバー・ステータスバーを、枠や影のないフラットな見た目にする</summary>
    internal sealed class ModernRenderer : ToolStripProfessionalRenderer
    {
        public ModernRenderer() : base(new ModernColors())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // メニューバーは下に、ステータスバーは上に、細い線だけ引く
            using var pen = new Pen(Theme.Border);
            int y = e.ToolStrip is StatusStrip ? 0 : e.ToolStrip.Height - 1;
            if (e.ToolStrip is ToolStripDropDown)
            {
                e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                return;
            }
            e.Graphics.DrawLine(pen, 0, y, e.ToolStrip.Width, y);
        }

        private sealed class ModernColors : ProfessionalColorTable
        {
            public ModernColors() { UseSystemColors = false; }

            public override Color MenuStripGradientBegin => Theme.Surface;
            public override Color MenuStripGradientEnd => Theme.Surface;
            public override Color ToolStripDropDownBackground => Theme.Surface;
            public override Color ImageMarginGradientBegin => Theme.Surface;
            public override Color ImageMarginGradientMiddle => Theme.Surface;
            public override Color ImageMarginGradientEnd => Theme.Surface;
            public override Color MenuItemSelected => Theme.Hover;
            public override Color MenuItemSelectedGradientBegin => Theme.Hover;
            public override Color MenuItemSelectedGradientEnd => Theme.Hover;
            public override Color MenuItemPressedGradientBegin => Theme.Hover;
            public override Color MenuItemPressedGradientMiddle => Theme.Hover;
            public override Color MenuItemPressedGradientEnd => Theme.Hover;
            public override Color MenuItemBorder => Theme.Hover;
            public override Color MenuBorder => Theme.Border;
            public override Color SeparatorDark => Theme.Border;
            public override Color SeparatorLight => Theme.Surface;
            public override Color StatusStripGradientBegin => Theme.Background;
            public override Color StatusStripGradientEnd => Theme.Background;
            public override Color ToolStripBorder => Theme.Border;
        }
    }
}
