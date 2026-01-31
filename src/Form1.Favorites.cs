using System;
using System.Linq;
using System.Windows.Forms;

namespace team1
{
    public partial class Form1
    {
        private void UpdateFavoriteButtons()
        {
            var isFavorite = _currentFavoriteKey != null && _favoriteKeys.Contains(_currentFavoriteKey);
            buttonFavorite.Enabled = _currentFavoriteKey != null && !isFavorite;
            buttonUnfavorite.Enabled = _currentFavoriteKey != null && isFavorite;
            labelFavoriteStatus.Text = isFavorite ? "お気に入り: 済" : "お気に入り: 未";
        }

        private void UpdateFavoriteMap()
        {
            _mapViewer.FavoritePlaceIds = _favoriteKeys.Where(id => id.StartsWith("room_", StringComparison.OrdinalIgnoreCase)).ToHashSet();
            _mapViewer.Invalidate();
            RefreshFavoriteList();
        }

        private void OnFavoriteAddClick(object sender, EventArgs e)
        {
            if (_currentFavoriteKey == null) return;
            _favoriteKeys.Add(_currentFavoriteKey);
            UpdateFavoriteButtons();
            UpdateFavoriteMap();
        }

        private void OnFavoriteRemoveClick(object sender, EventArgs e)
        {
            if (_currentFavoriteKey == null) return;
            _favoriteKeys.Remove(_currentFavoriteKey);
            UpdateFavoriteButtons();
            UpdateFavoriteMap();
        }

        private void RefreshFavoriteList()
        {
            var favorites = _favoriteKeys
                .Where(id => id.StartsWith("room_", StringComparison.OrdinalIgnoreCase))
                .Select(id => _places.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .DistinctBy(p => p!.Id)
                .Select(p => new SuggestionItem(p!))
                .ToList();

            listBoxFavorites.BeginUpdate();
            listBoxFavorites.Items.Clear();
            foreach (var item in favorites)
            {
                listBoxFavorites.Items.Add(item);
            }
            listBoxFavorites.EndUpdate();

            var hasItems = listBoxFavorites.Items.Count > 0;
            listBoxFavorites.Visible = hasItems;
            buttonFavoriteToStart.Enabled = hasItems;
            buttonFavoriteToGoal.Enabled = hasItems;
        }

        private void OnFavoriteToStartClick(object? sender, EventArgs e)
            => ApplyFavoriteToTextBox(textBox1);

        private void OnFavoriteToGoalClick(object? sender, EventArgs e)
            => ApplyFavoriteToTextBox(textBox2);

        private void OnFavoriteListDoubleClick(object? sender, EventArgs e)
            => ApplyFavoriteToTextBox(textBox1);

        private void OnFavoriteListKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            ApplyFavoriteToTextBox(textBox1);
            e.SuppressKeyPress = true;
        }

        private void ApplyFavoriteToTextBox(TextBox target)
        {
            if (listBoxFavorites.SelectedItem is not SuggestionItem item) return;

            target.Text = item.Place.Name;
            target.SelectionStart = target.Text.Length;
        }
    }
}
