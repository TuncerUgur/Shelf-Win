using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Interop;
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

        // Interop for Acrylic Blur
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }
        private DispatcherTimer _hoverTimer;
        private DockItem _hoveredFolderItem;
        private System.Windows.Media.Animation.Storyboard _currentThemeStoryboard;

        public static readonly DependencyProperty CurrentIconSizeProperty =
            DependencyProperty.Register("CurrentIconSize", typeof(double), typeof(MainWindow), new PropertyMetadata(36.0));

        public double CurrentIconSize
        {
            get { return (double)GetValue(CurrentIconSizeProperty); }
            set { SetValue(CurrentIconSizeProperty, value); }
        }

        public static readonly DependencyProperty ShowLabelsProperty =
            DependencyProperty.Register("ShowLabels", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool ShowLabels
        {
            get { return (bool)GetValue(ShowLabelsProperty); }
            set { SetValue(ShowLabelsProperty, value); }
        }

        public MainWindow(ShelfConfig config)
        {
            InitializeComponent();
            _config = config;
            _app = (App)System.Windows.Application.Current;
            Items = new ObservableCollection<DockItem>();
            DockItemsControl.ItemsSource = Items;

            _hoverTimer = new DispatcherTimer();
            _hoverTimer.Interval = TimeSpan.FromSeconds(2);
            _hoverTimer.Tick += HoverTimer_Tick;

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

            this.SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Removed Acrylic Blur because it causes square bounds bleed on rounded windows.
        }

        private void EnableAcrylicBlur()
        {
            var windowHelper = new WindowInteropHelper(this);
            var accent = new AccentPolicy();
            // Optional: You can check _config.ThemeName here to only enable if "ModernGlass", 
            // but we leave it globally enabled since other themes have opaque backgrounds that cover it.
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND; 
            
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        public void ApplySettingsState()
        {
            CurrentIconSize = _config.IconSize > 0 ? _config.IconSize : 36.0;
            ShowLabels = _config.ShowItemNames;

            // Apply Shelf Name
            ShelfNameTextBlock.Text = _config.ShelfName;
            ShelfNameTextBlock.Visibility = string.IsNullOrWhiteSpace(_config.ShelfName) ? Visibility.Collapsed : Visibility.Visible;

            // Apply Theme
            string themeName = string.IsNullOrEmpty(_config.ThemeName) ? "ModernGlass" : _config.ThemeName;
            try
            {
                if (_currentThemeStoryboard != null)
                {
                    _currentThemeStoryboard.Stop(GlassBorder);
                    _currentThemeStoryboard = null;
                }

                var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/Themes/{themeName}.xaml") };
                this.Resources.MergedDictionaries.Clear();
                this.Resources.MergedDictionaries.Add(dict);

                Dispatcher.InvokeAsync(() => 
                {
                    try {
                        if (this.Resources.Contains("ThemeAnimation"))
                        {
                            _currentThemeStoryboard = this.TryFindResource("ThemeAnimation") as System.Windows.Media.Animation.Storyboard;
                            if (_currentThemeStoryboard != null)
                            {
                                _currentThemeStoryboard.Begin(GlassBorder, true);
                            }
                        }
                    } catch (Exception ex) { Debug.WriteLine($"Animation Error: {ex.Message}"); }
                }, DispatcherPriority.Render);
            }
            catch (Exception ex) { 
                Debug.WriteLine($"Theme Load Error: {ex.Message}"); 
                // Fallback to minimal state if theme file missing/corrupt
            }

            // Apply Dock Color (Override if user specifically selected a custom color distinct from default glass)
            bool isCustomColor = !string.IsNullOrEmpty(_config.BackgroundColor) && _config.BackgroundColor != "#A1000000";
            bool isCustomImage = !string.IsNullOrEmpty(_config.BackgroundImagePath) && File.Exists(_config.BackgroundImagePath);

            // Block custom flat colors or old images from ruining advanced themes
            if (themeName != "ModernGlass" && themeName != "ZenMinimalist" && themeName != "MacStyle" && themeName != "DeepWoods")
            {
                isCustomColor = false; 
                isCustomImage = false; 
            }

            try {
                if (isCustomColor)
                {
                    GlassBorder.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_config.BackgroundColor));
                }
                else
                {
                    var themeBrush = this.TryFindResource("DockBackgroundBrush") as System.Windows.Media.Brush;
                    if (themeBrush != null) GlassBorder.Background = themeBrush;
                    else GlassBorder.SetResourceReference(Border.BackgroundProperty, "DockBackgroundBrush");
                }
            } catch {
                 // Final fallback - dark transparent
                 GlassBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(161, 0, 0, 0));
            }

            // Apply Background Image
            if (isCustomImage)
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
                HamburgerButton.Visibility = Visibility.Collapsed;
                MenuPanel.Visibility = Visibility.Visible;
                MenuOrientationButton.Visibility = Visibility.Collapsed;
                ControlsStackPanel.MaxWidth = double.PositiveInfinity;
            }
            else
            {
                DockItemsControl.ItemsPanel = _config.IsVertical ? (System.Windows.Controls.ItemsPanelTemplate)FindResource("VerticalPanel") : (System.Windows.Controls.ItemsPanelTemplate)FindResource("HorizontalPanel");
                
                if (_config.IsVertical)
                {
                    HamburgerButton.Visibility = Visibility.Visible;
                    MenuPanel.Visibility = Visibility.Collapsed; // Initial state: closed
                    ControlsStackPanel.MaxWidth = 40;
                    MenuOrientationButton.Content = "↔";
                    MenuOrientationButton.Visibility = Visibility.Visible;
                }
                else
                {
                    HamburgerButton.Visibility = Visibility.Collapsed;
                    MenuPanel.Visibility = Visibility.Visible;
                    ControlsStackPanel.MaxWidth = double.PositiveInfinity;
                    MenuOrientationButton.Content = "↕";
                    MenuOrientationButton.Visibility = Visibility.Visible;
                }
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

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            // We are in vertical mode and the main hamburger button was clicked: Toggle menu
            bool isCurrentlyOpen = MenuPanel.Visibility == Visibility.Visible;
            MenuPanel.Visibility = isCurrentlyOpen ? Visibility.Collapsed : Visibility.Visible;
            ControlsStackPanel.MaxWidth = isCurrentlyOpen ? 40 : double.PositiveInfinity;
        }

        private void MenuOrientationButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle orientation
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

            var wrapper = new DragDataWrapper { SourceWindow = this, Item = item };
            _draggedItem = item;
            _isDragging = true;
            try
            {
                DragDrop.DoDragDrop(el, wrapper, System.Windows.DragDropEffects.Move);
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
            var wrapper = e.Data.GetData(typeof(DragDataWrapper)) as DragDataWrapper;
            if (wrapper == null || wrapper.Item == null) return;
            
            var droppedItem = wrapper.Item;
            var sourceWindow = wrapper.SourceWindow;

            if (sender is FrameworkElement el && el.DataContext is DockItem target &&
                !ReferenceEquals(droppedItem, target))
            {
                int dstIdx = Items.IndexOf(target);
                if (dstIdx >= 0)
                {
                    if (sourceWindow != this)
                    {
                        // Cross-window drag
                        sourceWindow.Items.Remove(droppedItem);
                        sourceWindow._config.Items.Remove(droppedItem);
                        if (sourceWindow.Items.Count == 0) sourceWindow.DragZone.Visibility = Visibility.Visible;

                        Items.Insert(dstIdx, droppedItem);
                        _config.Items.Insert(dstIdx, droppedItem);
                        DragZone.Visibility = Visibility.Collapsed;
                        _app.SaveConfigs();
                    }
                    else
                    {
                        // Internal move
                        int srcIdx = Items.IndexOf(droppedItem);
                        if (srcIdx >= 0)
                        {
                            Items.Move(srcIdx, dstIdx);
                            _config.Items.Clear();
                            foreach (var i in Items) _config.Items.Add(i);
                            _app.SaveConfigs();
                        }
                    }
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
            if (e.Data.GetDataPresent(typeof(DragDataWrapper)) || e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) || e.Data.GetDataPresent(System.Windows.DataFormats.Text))
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

            // Handle Cross-Window dropping on empty space
            var wrapper = e.Data.GetData(typeof(DragDataWrapper)) as DragDataWrapper;
            if (wrapper != null && wrapper.Item != null && wrapper.SourceWindow != this)
            {
                var droppedItem = wrapper.Item;
                wrapper.SourceWindow.Items.Remove(droppedItem);
                wrapper.SourceWindow._config.Items.Remove(droppedItem);
                if (wrapper.SourceWindow.Items.Count == 0) wrapper.SourceWindow.DragZone.Visibility = Visibility.Visible;

                Items.Add(droppedItem);
                _config.Items.Add(droppedItem);
                addedNew = true;
            }
            // Handle Text or URLs
            else if (e.Data.GetDataPresent(System.Windows.DataFormats.Text))
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

                    bool isDir = Directory.Exists(file);
                    var item = new DockItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        FilePath = file,
                        Type = isDir ? DockItemType.Folder : (file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? DockItemType.Executable : DockItemType.File)
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
            _hoverTimer.Stop(); // Ensure popup doesn't open if clicked
            if (sender is FrameworkElement element && element.DataContext is DockItem item)
            {
                try
                {
                    if (item.Type == DockItemType.Executable || item.Type == DockItemType.File || item.Type == DockItemType.Folder)
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

        private void DockItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is FrameworkElement el && el.DataContext is DockItem item && item.Type == DockItemType.Folder)
            {
                _hoveredFolderItem = item;
                _hoverTimer.Start();
            }
        }

        private void DockItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _hoverTimer.Stop();
            _hoveredFolderItem = null;
        }

        private void HoverTimer_Tick(object sender, EventArgs e)
        {
            _hoverTimer.Stop();
            if (_hoveredFolderItem != null && _hoveredFolderItem.Type == DockItemType.Folder)
            {
                ShowFolderPreview(_hoveredFolderItem);
            }
        }

        private void ShowFolderPreview(DockItem folderItem)
        {
            FolderPreviewList.Children.Clear();
            if (!Directory.Exists(folderItem.FilePath)) return;

            try
            {
                var header = new TextBlock { Text = folderItem.Name, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(5, 5, 5, 10), TextTrimming = TextTrimming.CharacterEllipsis };
                FolderPreviewList.Children.Add(header);

                var entries = Directory.GetFileSystemEntries(folderItem.FilePath);
                int count = 0;
                foreach (var entry in entries)
                {
                    if (count >= 50) 
                    {
                        FolderPreviewList.Children.Add(new TextBlock { Text = "... Daha fazla", Foreground = System.Windows.Media.Brushes.Gray, FontStyle = FontStyles.Italic, Margin = new Thickness(5) });
                        break;
                    }

                    string name = Path.GetFileName(entry);
                    bool isDir = Directory.Exists(entry);
                    
                    var btn = new System.Windows.Controls.Button
                    {
                        Content = (isDir ? "📁 " : "📄 ") + name,
                        Background = System.Windows.Media.Brushes.Transparent,
                        Foreground = System.Windows.Media.Brushes.White,
                        BorderThickness = new Thickness(0),
                        HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
                        Padding = new Thickness(5),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        ToolTip = name
                    };
                    btn.Click += (s, ev) => 
                    {
                        try { Process.Start(new ProcessStartInfo(entry) { UseShellExecute = true }); } catch {}
                        FolderPreviewPopup.IsOpen = false;
                    };
                    FolderPreviewList.Children.Add(btn);

                    count++;
                }

                FolderPreviewPopup.IsOpen = true;
            }
            catch {}
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
            // Create thumbnail for images
            string ext = Path.GetExtension(filePath)?.ToLower();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif")
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.DecodePixelWidth = 128; // high-quality thumbnail constraint
                    bmp.UriSource = new Uri(filePath);
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
                catch {} // fallback if invalid image
            }

            return IconExtractor.GetStandardIcon(filePath);
        }
        private void AboutButton_Click(object sender, RoutedEventArgs e)
{
    AboutWindow aboutWin = new AboutWindow();
    aboutWin.Owner = this; // Ana pencerenin üzerinde düzgün görünmesi için
    aboutWin.ShowDialog(); // Kullanıcı pencereyi kapatana kadar ana pencereyi bekletir
}
    }

    public static class IconExtractor
    {
        public static ImageSource GetStandardIcon(string path)
        {
            try 
            {
                using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(path))
                {
                    if (icon != null)
                    {
                        var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        source.Freeze();
                        return source;
                    }
                }
            } 
            catch {}
            
            // For folders, ExtractAssociatedIcon might fail, so fallback
            return null;
        }
    }

    public class DragDataWrapper
    {
        public MainWindow SourceWindow { get; set; }
        public DockItem Item { get; set; }
    }
}