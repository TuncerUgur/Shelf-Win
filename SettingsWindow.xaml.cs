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
        private System.Windows.Media.Color _tempBgColor;

        public SettingsWindow(MainWindow mainWindow, ShelfConfig config)
        {
            InitializeComponent();
            _config = config;
            _mainWindow = mainWindow;
            
            _tempBgPath = _config.BackgroundImagePath;
            try 
            {
                _tempBgColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_config.BackgroundColor ?? "#A1000000");
            } 
            catch { _tempBgColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A1000000"); }
            
            NameTextBox.Text = _config.ShelfName ?? "";
            RowsTextBox.Text = _config.MatrixRows.ToString();
            ColumnsTextBox.Text = _config.MatrixColumns.ToString();
            IconSizeSlider.Value = _config.IconSize > 0 ? _config.IconSize : 36;
            ShowItemNamesCheckBox.IsChecked = _config.ShowItemNames;
            
            // Set combo box selection
            foreach (System.Windows.Controls.ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag.ToString() == (_config.ThemeName ?? "ModernGlass"))
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }

            UpdateBgPathText();
            UpdateColorPreview();
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

        private void SelectColorButton_Click(object sender, RoutedEventArgs e)
        {
            using (var cd = new System.Windows.Forms.ColorDialog())
            {
                cd.Color = System.Drawing.Color.FromArgb(_tempBgColor.R, _tempBgColor.G, _tempBgColor.B);
                if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _tempBgColor = System.Windows.Media.Color.FromArgb(_tempBgColor.A, cd.Color.R, cd.Color.G, cd.Color.B);
                    UpdateColorPreview();
                }
            }
        }

        private void DefaultColorButton_Click(object sender, RoutedEventArgs e)
        {
            _tempBgColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#A1000000");
            UpdateColorPreview();
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ColorPreviewBorder != null) 
            {
                byte alpha = (byte)((e.NewValue / 100.0) * 255.0);
                _tempBgColor.A = alpha;
                UpdateColorPreview();
            }
        }

        private void UpdateColorPreview()
        {
            if (ColorPreviewBorder != null)
            {
                ColorPreviewBorder.Background = new System.Windows.Media.SolidColorBrush(_tempBgColor);
                if (OpacitySlider != null)
                {
                    OpacitySlider.Value = (_tempBgColor.A / 255.0) * 100.0;
                }
            }
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
            _config.IconSize = IconSizeSlider.Value;
            _config.BackgroundColor = _tempBgColor.ToString();
            _config.ShowItemNames = ShowItemNamesCheckBox.IsChecked ?? false;
            
            if (ThemeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedThemeItem)
            {
                _config.ThemeName = selectedThemeItem.Tag.ToString();
            }

            this.DialogResult = true;
            this.Close();
        }
    }
}
