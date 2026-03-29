using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using DockShelf.Models;

namespace DockShelf
{
    public partial class App : System.Windows.Application
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private string configPath;
        
        public List<ShelfConfig> Configs { get; set; } = new List<ShelfConfig>();
        public List<MainWindow> OpenShelves { get; set; } = new List<MainWindow>();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Paths
            var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DockShelf");
            Directory.CreateDirectory(appDataDir);
            configPath = Path.Combine(appDataDir, "config.json");

            // Tray setup
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            
            // Try to use our custom icon
            var icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icon.ico");
            try
            {
                if (File.Exists(icoPath))
                    _notifyIcon.Icon = new System.Drawing.Icon(icoPath);
                else
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            catch
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "DockShelf";

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("New Shelf", null, (s, ev) => CreateNewShelf());
            contextMenu.Items.Add("Exit", null, (s, ev) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;

            // Load configs
            LoadConfigs();

            if (Configs.Count == 0)
            {
                CreateNewShelf();
            }
            else
            {
                foreach (var config in Configs)
                {
                    var window = new MainWindow(config);
                    OpenShelves.Add(window);
                    window.Show();
                }
            }
        }

        public void CreateNewShelf()
        {
            var config = new ShelfConfig();
            Configs.Add(config);
            
            var window = new MainWindow(config);
            OpenShelves.Add(window);
            window.Show();
            
            SaveConfigs();
        }

        public void SaveConfigs()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Configs, options);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save config: {ex.Message}", "DockShelf Error");
            }
        }

        private void LoadConfigs()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    Configs = JsonSerializer.Deserialize<List<ShelfConfig>>(json) ?? new List<ShelfConfig>();
                }
                catch
                {
                    Configs = new List<ShelfConfig>();
                }
            }
        }

        public void RemoveShelf(MainWindow window, ShelfConfig config)
        {
            Configs.Remove(config);
            OpenShelves.Remove(window);
            SaveConfigs();
            
            if (Configs.Count == 0)
            {
               // We could close, or just leave it running in tray.
            }
        }

        private void ExitApplication()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            SaveConfigs();
            System.Windows.Application.Current.Shutdown();
        }
    }
}
