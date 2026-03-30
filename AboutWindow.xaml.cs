using System.Diagnostics;
using System.Windows;

namespace DockShelf
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void OpenWebsite_Click(object sender, RoutedEventArgs e)
        {
            // Kendi sitenin linkini buraya yaz
            Process.Start(new ProcessStartInfo("https://kacincihaftadayiz.com") { UseShellExecute = true });
        }

        private void OpenDonate_Click(object sender, RoutedEventArgs e)
        {
            // Arkadaşın üzerinden aldığın linki buraya yaz
            Process.Start(new ProcessStartInfo("https://www.buymeacoffee.com/ugurtuncer") { UseShellExecute = true });
        }
    }
}