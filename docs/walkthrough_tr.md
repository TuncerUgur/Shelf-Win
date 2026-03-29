# DockShelf (WPF) - İzlenecek Yol (Walkthrough)

Windows Dock uygulamasının ilk geliştirme aşaması tamamlandı. Aşağıda nelerin başarıldığına, temel özelliklere ve bunların nasıl uygulandığına dair bir özet yer almaktadır.

## Neler Başarıldı?

1. **Hafif WPF Projesi*   **Çoklu Raf Yönetimi:** `System Tray` ikonundan sağ tıklayıp istediğiniz kadar yeni raf açabilme.
*   **Tam Özelleştirme İkonları:** Artık sadece sağ tıkla değil, rafın üstündeki **`+`** (Yeni Raf Ekle), **`↔/↕`** (Yatay/Dikey Yön Belirleme), **`🔓/🔒`** (Hareket Kilidi) ve **`📌`** (Her Zaman Üstte) ikonlarıyla tüm kontrolü hızlıca sağlayabilirsiniz.
*   **Dinamik Yatay/Dikey Tasarım:** Orientation butonuna bastığınızda simgelerin yan yana veya alt alta dizilmesi dinamik bir biçimde WrapPanel mimarisi uyarlanarak yapılır. Dikey konuma geçtiğinde yanlarda kalan boşluklar sıfırlanır, raf kendini tamamen daraltır!
*   **Mac Tadında 3 Boyutlu Etkileşim:** Raf boşken şıkça parlayan yarı transparan minik animasyonlu Glassmorphism paneli belirir. İçerikleriniz üzerinde fareyi gezdirdiğinizde yumuşakça büyüyüp ufalma (Scale Animasyonu) ile premium bir masaüstü hissi yaşatır. Özel Uygulama İkonu (`.ico`) da tasarıma şıklık katar.
2. **Cam Görüntüsü (Glassmorphism) UI Tasarımı:** XAML kenarlıkları (borders), gölgelendirme efektleri (dropshadow) ve saydam pencereler kullanarak yüzen, "macOS benzeri" bir raf oluşturduk. Arayüz sorunsuz bir şekilde diğer pencerelerin üstünde kalır ve Windows Görev Çubuğunu kirletmez (`ShowInTaskbar="False"`).
3. **Gelişmiş Sürükle ve Bırak Sistemi:**
   - Sürüklenip bırakılan verinin bir dosya (`FileDrop`) mı yoksa düz metin (`Text`) mi olduğunu değerlendirir.
   - Bildiğimiz Windows uygulamalarının (`.exe`), uygulama simgeleri `System.Drawing.Icon.ExtractAssociatedIcon` kullanılarak otomatik olarak çıkartılır. Böylece dock üzerinde tıklanabilir bir kısayol olarak simgesiyle birlikte anında görünürler.
   - Bırakılan metinler kısaltılarak "Txt" baloncukları olarak gösterilir.
4. **Çalıştırma ve Notepad Entegrasyonu:**
   - Bir dosyaya veya `.exe`'ye tek tıklamak, onu doğrudan işletim sistemi üzerinden başlatır (`Process.Start`).
   - Bir metin simgesine tek tıklamak, metin içeriğini yerel ve geçici bir `.txt` dosyasına aktarır ve dosya doğrudan `notepad.exe` ile açılır.

## Teknik Özet
- **Değiştirilen/Oluşturulan Dosyalar:**
  - `DockShelf.csproj`: Çalıştırılabilir (exe) dosyalardan simge çıkarmak için gerekli olan `System.Windows.Forms` desteği eklenecek şekilde güncellendi.
  - `App.xaml.cs`: Windows Forms ile isim çakışmasını önlemek için uygun `Application` referans sınırları sağlandı.
  - `MainWindow.xaml`: Dock estetiği ve cam görünümü için XAML kullanıcı arayüzü (UI) tanımlamaları yapıldı.
  - `MainWindow.xaml.cs`: Sürükle ve bırak olayları (drag & drop), pencere sürükleme özellikleri ve diğer programların çalıştırılması (Process execution) gibi ana işlevlerin (heavy lifting) kodlandığı bölüm.
  - `Models/DockItem.cs`: Dock'a eklenen her bir öğenin (dosya, exe, metin) verisini temsil eden yapısal model sınıfı.

> [!TIP]
> **Performans İyileştirmeleri:** İlk fikrimiz olan Electron yerine WPF'yi seçerek, arka planda çalışma sırasındaki RAM tüketimini önemli ölçüde azalttık ve Windows'un kendi sistem dillerini kullandığımız için dosya sürükle bırak gibi işlemler hiçbir gecikme yaşamadan anında gerçekleşiyor.

## Doğrulama Listesi (Testler)

Proje sorunsuz olarak derlenmektedir. Arka planda Windows klasör sistemine, .exe yapılarına ve masaüstü API'lerine yerel (native) olarak mükemmel uyum sağlar.
Manuel olarak da doğrulamak isterseniz, klasörünüze geri dönün ve şu komutu çalıştırın:
```powershell
dotnet run
```
Bu adımla birlikte program başlayacak; ardında masaüstünüzden .exe dosyalarını bu cam rafın içerisine doğrudan sürüklerken keyfini çıkarabilirsiniz!
