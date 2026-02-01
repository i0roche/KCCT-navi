using System;

namespace team1
{
    public partial class Form1
    {
        private void ShowSearchPanel()
        {
            panel1.Visible = true;
            panel1.BringToFront();
            MenuStrip1.BringToFront();
            panelSearch.Visible = true;
            panelBuilding.Visible = false;

            RefreshFavoriteList();

            buttonTabSearch.Enabled = false;
            buttonTabInfo.Enabled = true;
        }

        private void ShowBuildingPanel(string title, string description, string favoriteKey)
        {
            panel1.Visible = true;
            panel1.BringToFront();
            MenuStrip1.BringToFront();
            panelSearch.Visible = false;
            panelBuilding.Visible = true;

            labelBuildingTitle.Text = title;
            labelBuildingDescription.Text = description;

            _currentInfoTitle = title;
            _currentInfoDescription = description;
            _currentFavoriteKey = favoriteKey;

            UpdateFavoriteButtons();

            buttonTabSearch.Enabled = true;
            buttonTabInfo.Enabled = false;
        }

        private void ShowEmptyInfoPanel()
        {
            panel1.Visible = true;
            panel1.BringToFront();
            MenuStrip1.BringToFront();
            panelSearch.Visible = false;
            panelBuilding.Visible = true;

            labelBuildingTitle.Text = "未選択";
            labelBuildingDescription.Text = "建物または部屋を選択してください。";
            labelFavoriteStatus.Text = "お気に入り: -";

            _currentInfoTitle = null;
            _currentInfoDescription = null;
            _currentFavoriteKey = null;

            buttonFavorite.Enabled = false;
            buttonUnfavorite.Enabled = false;

            buttonTabSearch.Enabled = true;
            buttonTabInfo.Enabled = false;
        }
    }
}
