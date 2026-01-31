using System;
using System.Linq;
using System.Windows.Forms;

namespace team1
{
    public partial class Form1
    {
        private void UpdateFavoriteButtons() // お気に入りボタンの状態を更新
        {
            var isFavorite = _currentFavoriteKey != null && _favoriteKeys.Contains(_currentFavoriteKey);
            buttonFavorite.Enabled = _currentFavoriteKey != null && !isFavorite;
            buttonUnfavorite.Enabled = _currentFavoriteKey != null && isFavorite;
            labelFavoriteStatus.Text = isFavorite ? "お気に入り: 済" : "お気に入り: 未";
        }

        private void UpdateFavoriteMap() // マップビューアのお気に入り情報を更新
        {
            _mapViewer.FavoritePlaceIds = _favoriteKeys.Where(id => id.StartsWith("room_", StringComparison.OrdinalIgnoreCase)).ToHashSet();
            _mapViewer.Invalidate();
            RefreshFavoriteList();
        }

        private void OnFavoriteAddClick(object sender, EventArgs e) // お気に入り追加ボタンのクリックイベントハンドラ
        {
            if (_currentFavoriteKey == null) return;
            _favoriteKeys.Add(_currentFavoriteKey);
            UpdateFavoriteButtons();
            UpdateFavoriteMap();
        }

        private void OnFavoriteRemoveClick(object sender, EventArgs e) //お気に入り削除ボタンのクリックイベントハンドラ
        {
            if (_currentFavoriteKey == null) return;
            _favoriteKeys.Remove(_currentFavoriteKey);
            UpdateFavoriteButtons();
            UpdateFavoriteMap();
        }

        private void RefreshFavoriteList() // お気に入りリストを更新
        {
            var favorites = _favoriteKeys
                .Where(id => id.StartsWith("room_", StringComparison.OrdinalIgnoreCase))// お気に入りは部屋のみ
                .Select(id => _places.FirstOrDefault(p => p.Id == id))                  // IDからPlaceを取得
                .Where(p => p != null)                                                  // null除外
                .DistinctBy(p => p!.Id)                                                 // 重複除外
                .Select(p => new SuggestionItem(p!))                                    // SuggestionItemに変換
                .ToList();                                                              // リスト化

            listBoxFavorites.BeginUpdate(); // リストボックスの更新を一時停止
            listBoxFavorites.Items.Clear(); // 既存のアイテムをクリア
            foreach (var item in favorites) // 新しいアイテムを追加
            {
                listBoxFavorites.Items.Add(item);
            }
            listBoxFavorites.EndUpdate();

            var hasItems = listBoxFavorites.Items.Count > 0; // アイテムが存在すれば表示・有効化
            listBoxFavorites.Visible = hasItems;
            buttonFavoriteToStart.Enabled = hasItems;
            buttonFavoriteToGoal.Enabled = hasItems;
        }

        private void OnFavoriteToStartClick(object? sender, EventArgs e) // お気に入りを出発地テキストボックスに適用
            => ApplyFavoriteToTextBox(textBox1);

        private void OnFavoriteToGoalClick(object? sender, EventArgs e)// お気に入りを目的地テキストボックスに適用
            => ApplyFavoriteToTextBox(textBox2);

        private void OnFavoriteListDoubleClick(object? sender, EventArgs e)// お気に入りリストのダブルクリックイベントハンドラ
            => ApplyFavoriteToTextBox(textBox1);

        private void OnFavoriteListKeyDown(object? sender, KeyEventArgs e)// お気に入りリストのキー押下イベントハンドラ
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
