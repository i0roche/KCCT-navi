using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using team1.Models;

namespace team1
{
    public partial class Form1
    {
        private void DoSearchOrRoute()
        {
            HideSuggestionLists();

            var startQuery = textBox1.Text.Trim();
            var goalQuery = textBox2.Text.Trim();

            if (string.IsNullOrWhiteSpace(startQuery) && string.IsNullOrWhiteSpace(goalQuery))
            {
                MessageBox.Show("検索キーワードを入力してください。");
                return;
            }

            if (string.IsNullOrWhiteSpace(goalQuery))
            {
                DoSearch(startQuery);
                return;
            }

            if (string.IsNullOrWhiteSpace(startQuery))
            {
                MessageBox.Show("スタートを入力してください。");
                return;
            }

            DoRouteSearch(startQuery, goalQuery);
        }

        private void OnStartTextChanged(object? sender, EventArgs e)
        {
            // サジェストからの選択時はIDをクリアしない
            if (_selectedStartPlaceId == null || !textBox1.Text.Equals(_places.FirstOrDefault(p => p.Id == _selectedStartPlaceId)?.Name))
            {
                _selectedStartPlaceId = null;
            }
            UpdateSuggestions(textBox1, listBoxStart);
        }

        private void OnGoalTextChanged(object? sender, EventArgs e)
        {
            // サジェストからの選択時はIDをクリアしない
            if (_selectedGoalPlaceId == null || !textBox2.Text.Equals(_places.FirstOrDefault(p => p.Id == _selectedGoalPlaceId)?.Name))
            {
                _selectedGoalPlaceId = null;
            }
            UpdateSuggestions(textBox2, listBoxGoal);
        }

        private void UpdateSuggestions(TextBox source, ListBox listBox)
        {
            var query = source.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                listBox.Visible = false;
                listBox.Items.Clear();
                return;
            }

            var matches = FindPlaces(query)
                .DistinctBy(p => p.Id)
                .Take(8)
                .Select(p => new SuggestionItem(p))
                .ToList();

            listBox.BeginUpdate();
            listBox.Items.Clear();
            foreach (var item in matches)
            {
                listBox.Items.Add(item);
            }
            listBox.EndUpdate();

            listBox.Visible = listBox.Items.Count > 0;
        }

        private void OnStartSuggestionDoubleClick(object? sender, EventArgs e)
            => ApplySuggestion(textBox1, listBoxStart);

        private void OnGoalSuggestionDoubleClick(object? sender, EventArgs e)
            => ApplySuggestion(textBox2, listBoxGoal);

        private void OnSuggestionKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            if (sender == listBoxStart)
            {
                ApplySuggestion(textBox1, listBoxStart);
            }
            else if (sender == listBoxGoal)
            {
                ApplySuggestion(textBox2, listBoxGoal);
            }

            e.SuppressKeyPress = true;
        }

        private void ApplySuggestion(TextBox target, ListBox listBox)
        {
            if (listBox.SelectedItem is not SuggestionItem item) return;

            // 先にIDを設定（TextChangedより前に）
            if (target == textBox1)
                _selectedStartPlaceId = item.Place.Id;
            else if (target == textBox2)
                _selectedGoalPlaceId = item.Place.Id;

            // テキスト設定（TextChangedが発火するがIDはクリアされない）
            target.Text = item.Place.Name;
            target.SelectionStart = target.Text.Length;
            listBox.Visible = false;    
        }

        private void HideSuggestionLists()
        {
            listBoxStart.Visible = false;
            listBoxGoal.Visible = false;
        }

        private sealed record SuggestionItem(Place Place)
        {
            public override string ToString()
            {
                var name = string.IsNullOrWhiteSpace(Place.Name) ? Place.Id : Place.Name;
                var building = string.IsNullOrWhiteSpace(Place.Building) ? "" : Place.Building;
                var floor = Place.Floor > 0 ? $"{Place.Floor}F" : "";
                return $"{name} ({building},{floor})";
            }
        }

        private void DoSearch(string query)
        {
            var matches = FindPlaces(query).ToList();
            if (matches.Count == 0)
            {
                MessageBox.Show($"\"{query}\" は見つかりませんでした。");
                return;
            }

            _mapViewer.CurrentRoute = null;

            var selected = SelectBestMatch(query, matches);
            FocusPlace(selected);

            if (matches.Count > 1)
            {
                MessageBox.Show($"\"{query}\" は {matches.Count} 件見つかりました。候補「{selected.Name}」に移動しました。");
            }
        }

        private void DoRouteSearch(string startQuery, string goalQuery)
        {
            if (!TryResolvePlace(startQuery, _selectedStartPlaceId, out var start) || !TryResolvePlace(goalQuery, _selectedGoalPlaceId, out var goal))
            {
                return;
            }

            var route = _pathFinder.FindPath(start.Id, goal.Id);
            if (route == null || route.Count == 0)
            {
                MessageBox.Show("経路が見つかりませんでした。");
                return;
            }

            _mapViewer.CurrentRoute = route;
            UpdateMapDisplay(start.Floor);
            _mapViewer.Invalidate();

            ShowSearchPanel();
        }

        private IEnumerable<Place> FindPlaces(string query)
        {
            return _places.Where(place => IsMatch(place, query));
        }

        private bool TryResolvePlace(string query, string? selectedId, out Place place)
        {
            place = null!;

            // サジェストから選択したIDがあればそれを使う
            if (!string.IsNullOrEmpty(selectedId))
            {
                var found = _places.FirstOrDefault(p => p.Id == selectedId);
                if (found != null)
                {
                    place = found;
                    return true;
                }
            }

            // 従来の名前検索にフォールバック
            var matches = FindPlaces(query).ToList();
            if (matches.Count == 0)
            {
                MessageBox.Show($"\"{query}\" は見つかりませんでした。");
                return false;
            }

            place = SelectBestMatch(query, matches);
            return true;
        }

        private static Place SelectBestMatch(string query, List<Place> matches)
        {
            return matches.FirstOrDefault(place => place.Name.Equals(query, StringComparison.OrdinalIgnoreCase))
                ?? matches.FirstOrDefault(place => place.Id.Equals(query, StringComparison.OrdinalIgnoreCase))
                ?? matches.FirstOrDefault(place => place.Tags.Any(tag => tag.Equals(query, StringComparison.OrdinalIgnoreCase)))
                ?? matches[0];
        }

        private static bool IsMatch(Place place, string query)
        {
            return ContainsIgnoreCase(place.Name, query)
                || ContainsIgnoreCase(place.Id, query)
                || ContainsIgnoreCase(place.Type, query)
                || ContainsIgnoreCase(place.Building, query)
                || ContainsIgnoreCase(place.Department, query)
                || place.Tags.Any(tag => ContainsIgnoreCase(tag, query));
        }

        private static bool ContainsIgnoreCase(string? source, string query)
            => source?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

        private void OnSearchButtonClick(object sender, EventArgs e) => DoSearchOrRoute();

        private void OnSearchTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                DoSearchOrRoute();
            }
        }

        private void OnSearchPanelRequested(object? sender, EventArgs e) => ShowSearchPanel();

        private void OnSearchTabClick(object? sender, EventArgs e) => ShowSearchPanel();

        private void OnInfoTabClick(object? sender, EventArgs e)
        {
            if (_currentInfoTitle == null || _currentInfoDescription == null || _currentFavoriteKey == null)
            {
                ShowEmptyInfoPanel();
                return;
            }

            ShowBuildingPanel(_currentInfoTitle, _currentInfoDescription, _currentFavoriteKey);
        }
    }
}
