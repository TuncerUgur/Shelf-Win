using System;
using System.Windows;
using DockShelf.Models;
using Microsoft.Win32;

namespace DockShelf
{
    public partial class SettingsWindow : Window
    {
        private ShelfConfig _config;
        private MainWindow _mainWindow;
        private string _tempBgPath;

        public SettingsWindow(MainWindow mainWindow, ShelfConfig config)
        {
            InitializeComponent();
            _config = config;
            _mainWindow = mainWindow;
            
            _tempBgPath = _config.BackgroundImagePath;
            
            NameTextBox.Text = _config.ShelfName ?? "";
            RowsTextBox.Text = _config.MatrixRows.ToString();
            ColumnsTextBox.Text = _config.MatrixColumns.ToString();
            UpdateBgPathText();
        }

        private void SelectBgButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
            dlg.Title = "Raf Arka Planı Seç";

            if (dlg.ShowDialog() == true)
            {
                _tempBgPath = dlg.FileName;
                UpdateBgPathText();
            }
        }

        private void ClearBgButton_Click(object sender, RoutedEventArgs e)
        {
            _tempBgPath = null;
            UpdateBgPathText();
        }

        private void UpdateBgPathText()
        {
            if (string.IsNullOrEmpty(_tempBgPath))
                BgPathTextBlock.Text = "Seçili Görsel Yok";
            else
                BgPathTextBlock.Text = System.IO.Path.GetFileName(_tempBgPath);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Save name
            _config.ShelfName = NameTextBox.Text?.Trim() ?? "";

            // Parse and save matrix
            if (int.TryParse(RowsTextBox.Text, out int rows) && rows >= 0)
                _config.MatrixRows = rows;
            else
                _config.MatrixRows = 0;

            if (int.TryParse(ColumnsTextBox.Text, out int cols) && cols >= 0)
                _config.MatrixColumns = cols;
            else
                _config.MatrixColumns = 0;

            _config.BackgroundImagePath = _tempBgPath;

            this.DialogResult = true;
            this.Close();
        }
    }
}
