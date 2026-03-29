# Windows Dock/Shelf Uygulama Planı (WPF)

Bu projenin amacı, dosyalar, bağlantılar ve metin parçacıkları için dinamik bir "Dock" (İskele) veya "Shelf" (Raf) işlevi gören bir Windows masaüstü uygulaması oluşturmaktır. Kullanıcılar metinleri/bağlantıları/çalıştırılabilir dosyaları (exe) sürükleyip bırakabilmeli, bu dosyaları çalıştırabilmeli ve birden fazla dock oluşturabilmelidir.

**Güncelleme:** Geri bildirimlerinize dayanarak, Electron yerine **C# ile WPF (Windows Presentation Foundation)** kullanacağız. Bu temel olarak çok doğru bir karar: Electron'un Chromium sekmesi çok daha fazla bellek tüketirken, WPF Windows için yerel olarak derlenir, önemli ölçüde daha hızlı başlar, çok az RAM kullanır ve arka plan araçları için mükemmeldir.

## Önerilen Mimari ve Teknoloji Yığını

- **Ana Masaüstü Altyapısı:** .NET 8 üzerinde WPF (C#)
  - Windows kabuk (shell) API'lerine yerel erişim sağlar (`.exe` simgelerini çıkarmak ve metni Notepad'e göndermek için mükemmeldir).
  - Electron'a kıyasla SON DERECE hafif kaynak kullanımı sunar.
- **Arayüz (UI) Tasarımı:**
  - Çerçevesiz, arka planı şeffaf ve köşeleri yuvarlatılmış pencereler oluşturmak için XAML kullanımı.
  - Windows 10/11'de premium cam (Akrilik) efektleri için `System.Windows.Shell.WindowChrome` veya doğrudan Win32 API'lerinin kullanımı.

## Temel Özelliklerin Uygulanması

### 1. Çerçevesiz Akrilik Pencere
- WPF Penceresini `WindowStyle="None"` ve `AllowsTransparency="True"` olarak ayarlayacağız.
- Windows'ta "premium MacOS rafı" görünümü vermek için yerel çağrılar kullanarak Windows 11 Akrilik/Mica arka plan efektlerini uygulayabiliriz.

### 2. Sürükle ve Bırak (Drag & Drop) İşlevi
- WPF'in yerel `AllowDrop="True"` özelliği ve `Drop` olay dinleyicileri (event handlers) kullanılacak.
- **Metin/Bağlantılar:** `e.Data.GetData(DataFormats.Text)` ile alınacak.
- **Dosyalar (.exe):** `e.Data.GetData(DataFormats.FileDrop)` ile alınacak. Dosyayı rafta göstermek için `System.Drawing.Icon.ExtractAssociatedIcon` kullanarak uygulamanın gerçek simgesini dinamik olarak çıkartacağız.

### 3. Dosya Çalıştırma ve Notepad Entegrasyonu
- Çalıştırılabilir dosyalar (exe) C#'ın `System.Diagnostics.Process.Start(filepath)` metodu kullanılarak başlatılır.
- Metin damlacıkları: Tıklandığında, metin yükünü geçici bir `.txt` dosyasına kaydederiz ve `Process.Start("notepad.exe", tempFilePath)` komutunu çalıştırırız.

### 4. Birden Fazla Dock (Raf)
- Ana uygulama, istek üzerine yeni WPF `<Window>` (pencere) örnekleri üretebilen bir `DockManager`'a sahip olacak.
- PC yeniden başlatıldığında bile kalıcı olmaları için, tüm raflardaki öğeleri `AppData` klasörünüzdeki yerel bir `config.json` dosyasına kaydedeceğiz.
