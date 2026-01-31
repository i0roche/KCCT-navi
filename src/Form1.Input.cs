using System;
using System.Windows.Forms;

namespace team1
{
    public partial class Form1
    {
        private void OnFormKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                ShowSearchPanel();
                textBox1.Focus();
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.G)
            {
                ShowSearchPanel();
                textBox2.Focus();
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.Enter)
            {
                DoSearchOrRoute();
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                _mapViewer.Focus();
                e.SuppressKeyPress = true;
            }
        }
    }
}
