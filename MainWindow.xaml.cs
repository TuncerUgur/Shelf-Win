using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DockShelf.Models;

namespace DockShelf
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<DockItem> Items { get; set; }
        private ShelfConfig _config;
        private App _app;
        private bool _isEditMode = false;
        private DockItem _draggedItem = null;
        private bool _isDragging = false;
        private System.Windows.Point _dragStart;
        private bool _lockStateBeforeEdit = false; // saves lock before edit mode

        public MainWindow(ShelfConfig config)
        {
            InitializeComponent();
            _config = config;
            _app = (App)System.Windows.Application.Current;
            Items = new ObservableCollection<DockItem>();
            DockItemsControl.ItemsSource = Items;

            // Restore from config, but clamp to screen bounds
            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;

            if (_config.Left > 0 || _config.Top > 0)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                // Ensure window is within screen bounds
                this.Left = Math.Max(0, Math.Min(_config.Left, screenW - 150));
                this.Top  = Math.Max(0, Math.Min(_config.Top,  screenH - 50));
            }
            else
            {
                // Default: top-right corner, easy to find
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = screenW - 200;
                this.Top  = 50;
            }

            // Reload all items from the config to extract the IconImage (which wasn't saved in JSON)
            foreach (var item in _config.Items)
            {
                if (item.Type == DockItemType.Executable || item.Type == DockItemType.File)
                {
                    item.IconImage = GetIconForFile(item.FilePath);
                }
                Items.Add(item);
            }

            if (Items.Count > 0)
            {
                DragZone.Visibility = Visibility.Collapsed;
            }

            ApplyPinState();
            ApplySettingsState();
            ApplyLockState();
        }

        public void ApplySettingsState()
        {
            // Apply Background
            if (!string.IsNullOrEmpty(_config.BackgroundImagePath) && File.Exists(_config.BackgroundImagePath))
            {
                try
                {
                    var bitmap = new BitmapImage(new Uri(_config.BackgroundImagePath, UriKind.Absolute));
                    ImageBgBorder.Background = new ImageBrush(bitmap) { Stretch = System.Windows.Media.Stretch.UniformToFill };
                }
                catch { ImageBgBorder.Background = System.Windows.Media.Brushes.Transparent; }
            }
            else
            {
                ImageBgBorder.Background = System.Windows.Media.Brushes.Transparent;
            }

            // Apply Matrix
            if (_config.MatrixColumns > 0 || _config.MatrixRows > 0)
            {
                var factory = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.UniformGrid));
                factory.SetValue(FrameworkElement.VerticalAlignmentProperty, System.Windows.VerticalAlignment.Center);
                factory.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
                if (_config.MatrixColumns > 0) factory.SetValue(System.Windows.Controls.Primitives.UniformGrid.ColumnsProperty, _config.MatrixColumns);
                if (_config.MatrixRows > 0) factory.SetValue(System.Windows.Controls.Primitives.UniformGrid.RowsProperty, _config.MatrixRows);

                DockItemsControl.ItemsPanel = new System.Windows.Controls.ItemsPanelTemplate(factory);
                OrientationButton.Visibility = Visibility.Collapsed; 
                ControlsWrapPanel.MaxWidth = double.PositiveInfinity;
            }
            else
            {
                OrientationButton.Visibility = Visibility.Visible;
                DockItemsControl.ItemsPanel = _config.IsVertical ? (System.Windows.Controls.ItemsPanelTemplate)FindResource("VerticalPanel") : (System.Windows.Controls.ItemsPanelTemplate)FindResource("HorizontalPanel");
                OrientationButton.Content = _config.IsVertical ? "↕" : "↔";
                ControlsWrapPanel.MaxWidth = _config.IsVertical ? 40 : double.PositiveInfinity;
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            _config.Left = this.Left;
            _config.Top = this.Top;
            _app.SaveConfigs();
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            _config.IsPinned = !_config.IsPinned;
            ApplyPinState();
            _app.SaveConfigs();
        }

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            _config.IsLocked = !_config.IsLocked;
            ApplyLockState();
            _app.SaveConfigs();
        }

        private void OrientationButton_Click(object sender, RoutedEventArgs e)
        {
            _config.IsVertical = !_config.IsVertical;
            ApplySettingsState();
            _app.SaveConfigs();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var sw = new SettingsWindow(this, _config);
            sw.Owner = this;
            if (sw.ShowDialog() == true)
            {
                ApplySettingsState();
                _app.SaveConfigs();
            }
        }

        private void EditModeButton_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = !_isEditMode;

            if (_isEditMode)
            {
                // Save current lock state, then force-lock so raf doesn't move while reordering
                _lockStateBeforeEdit = _config.IsLocked;
                _config.IsLocked = true;
                ApplyLockState();
            }
            else
            {
                // Restore lock state from before edit mode
                _config.IsLocked = _lockStateBeforeEdit;
                ApplyLockState();
                _app.SaveConfigs();
            }

            DockItemsControl.ItemTemplate = (DataTemplate)FindResource(
                _isEditMode ? "DockItemEditTemplate" : "DockItemTemplate");
            EditModeButton.Foreground = _isEditMode
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 255, 255, 255));
        }

        // ── Edit mode: drag-to-reorder ─────────────────────────────────────
        public void EditItem_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isEditMode || _isDragging) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (!(sender is FrameworkElement el && el.DataContext is DockItem item)) return;

            var pos = e.GetPosition(null);
            var diff = pos - _dragStart;
            // Require at least 5px drag before initiating
            if (Math.Abs(diff.X) < 5 && Math.Abs(diff.Y) < 5) return;

            _draggedItem = item;
            _isDragging = true;
            try
            {
                DragDrop.DoDragDrop(el, item, System.Windows.DragDropEffects.Move);
            }
            finally
            {
                _isDragging = false;
            }
        }

        public void EditItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            e.Handled = true; // prevent window drag when clicking items
        }

        private void DockItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Just mark handled to prevent window drag from interfering with the MouseLeftButtonUp click
            e.Handled = true;
        }

        public void EditItem_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (_draggedItem == null) return;
            if (sender is FrameworkElement el && el.DataContext is DockItem target &&
                !ReferenceEquals(_draggedItem, target))
            {
                int srcIdx = Items.IndexOf(_draggedItem);
                int dstIdx = Items.IndexOf(target);
                if (srcIdx >= 0 && dstIdx >= 0)
                {
                    Items.Move(srcIdx, dstIdx);
                    _config.Items.Clear();
                    foreach (var i in Items) _config.Items.Add(i);
                    _app.SaveConfigs();
                }
            }
            _draggedItem = null;
        }

        public void DeleteEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.DataContext is DockItem item)
            {
                Items.Remove(item);
                _config.Items.Remove(item);
                if (Items.Count == 0) DragZone.Visibility = Visibility.Visible;
                _app.SaveConfigs();
            }
        }

        private void AddDockButton_Click(object sender, RoutedEventArgs e)
        {
            _app.CreateNewShelf();
        }

        private void ApplyPinState()
        {
            this.Topmost = _config.IsPinned;
            PinButton.Foreground = _config.IsPinned ? new SolidColorBrush(Colors.White) : new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 255, 255, 255));
        }

        private void ApplyLockState()
        {
            LockButton.Content = _config.IsLocked ? "🔒" : "🔓";
            LockButton.Foreground = _config.IsLocked ? new SolidColorBrush(Colors.White) : new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 255, 255, 255));
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && !_config.IsLocked)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _app.RemoveShelf(this, _config);
            this.Close();
        }

        private void Window_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) || e.Data.GetDataPresent(System.Windows.DataFormats.Text))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            DragZone.Visibility = Visibility.Collapsed;
            bool addedNew = false;

            // Capture drop position BEFORE adding item (for smart matrix logic)
            var dropPos = e.GetPosition(DockItemsControl);

            // Handle Text or URLs
            if (e.Data.GetDataPresent(System.Windows.DataFormats.Text))
            {
                string text = (string)e.Data.GetData(System.Windows.DataFormats.Text);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var item = new DockItem
                    {
                        Name = text.Length > 15 ? text.Substring(0, 15) + "..." : text,
                        TextContent = text,
                        Type = DockItemType.TextSnippet
                    };
                    Items.Add(item);
                    if (!_config.Items.Contains(item)) _config.Items.Add(item);
                    addedNew = true;
                }
            }
            // ── Duplicate file guard ───────────────────────────────────────
            else if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (string file in files)
                {
                    // Skip if same file path already exists
                    if (_config.Items.Exists(i => string.Equals(i.FilePath, file, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var item = new DockItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        FilePath = file,
                        Type = file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? DockItemType.Executable : DockItemType.File
                    };

                    item.IconImage = GetIconForFile(file);
                    Items.Add(item);
                    if (!_config.Items.Contains(item)) _config.Items.Add(item);
                    addedNew = true;
                }
            }

            if (addedNew)
            {
                // Smart Matrix Expansion: only when matrix mode is active
                TrySmartMatrixExpand(dropPos);
                ApplySettingsState();
                _app.SaveConfigs();
            }
        }

        /// <summary>
        /// When in matrix mode, checks DROP position to decide how to expand the grid:
        /// - Dropped near right edge  → add a column  (e.g. 2x3 → 2x4)
        /// - Dropped near bottom edge → add a row     (e.g. 2x3 → 3x3)
        /// </summary>
        private void TrySmartMatrixExpand(System.Windows.Point dropPos)
        {
            // Only act when matrix mode is explicitly configured
            if (_config.MatrixColumns <= 0 && _config.MatrixRows <= 0)
                return;

            int rows = _config.MatrixRows > 0 ? _config.MatrixRows : 1;
            int cols = _config.MatrixColumns > 0 ? _config.MatrixColumns : 1;
            int capacity = rows * cols;

            // If there's still room in the matrix, no expansion needed
            if (Items.Count <= capacity)
                return;

            // Get the rendered size of the items panel
            double panelW = DockItemsControl.ActualWidth;
            double panelH = DockItemsControl.ActualHeight;

            // Use 0 as fallback (first drop, panel not yet rendered)
            double relX = panelW > 0 ? dropPos.X / panelW : 0.5;
            double relY = panelH > 0 ? dropPos.Y / panelH : 0.5;

            // Right-edge drop  → grow horizontally (add column)
            // Bottom-edge drop → grow vertically   (add row)
            // If both, pick the dimension with the lower relative position
            bool nearRight  = relX > 0.75;
            bool nearBottom = relY > 0.75;

            if (nearRight && !nearBottom)
            {
                _config.MatrixColumns = cols + 1;
            }
            else if (nearBottom && !nearRight)
            {
                _config.MatrixRows = rows + 1;
            }
            else if (nearRight && nearBottom)
            {
                // Corner drop: expand in the direction with more pressure
                if (relX >= relY)
                    _config.MatrixColumns = cols + 1;
                else
                    _config.MatrixRows = rows + 1;
            }
            else
            {
                // Middle / ambiguous: default expand rows (taller feels more natural)
                _config.MatrixRows = rows + 1;
            }
        }

        private void DockItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is DockItem item)
            {
                try
                {
                    if (item.Type == DockItemType.Executable || item.Type == DockItemType.File)
                    {
                        Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
                    }
                    else if (item.Type == DockItemType.TextSnippet)
                    {
                        string tempFile = Path.Combine(Path.GetTempPath(), $"DockShelfText_{Guid.NewGuid()}.txt");
                        File.WriteAllText(tempFile, item.TextContent);
                        Process.Start(new ProcessStartInfo("notepad.exe", tempFile) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to open item: {ex.Message}", "Error");
                }
            }
        }

        // Add Delete Option using Right Click
        private void DockItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is DockItem item)
            {
                Items.Remove(item);
                _config.Items.Remove(item);
                
                if (Items.Count == 0)
                {
                    DragZone.Visibility = Visibility.Visible;
                }
                
                _app.SaveConfigs();
            }
        }

        private ImageSource GetIconForFile(string filePath)
        {
            try
            {
                using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(filePath))
                {
                    if (icon != null)
                    {
                        return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                    }
                }
            }
            catch
            {
                // Fallback icon if extraction fails
            }
            return null;
        }
    }
}