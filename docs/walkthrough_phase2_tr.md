# DockShelf (WPF) - İzlenecek Yol Aşama 2 (Walkthrough Phase 2)

Yerel geliştirmenin 2. Aşaması tamamlandı ve istenen tüm özellik geliştirmeleri başarıyla eklendi!

## Neler Başarıldı? (Aşama 2)

1. **Sistem Tepsisi (System Tray) Entegrasyonu:** Uygulama artık Windows Görev Çubuğunun sağ alt kısmına (Ağ, ses ikonlarının yanına) doğrudan sessizce yüklenir. İkona sağ tıklayarak "Yeni Raf Oluştur (New Shelf)" diyebilir veya uygulamadan tamamen çıkış yapabilirsiniz.
2. **Kalıcı JSON Verisi:** Raflara sürükleyip bıraktığınız tüm programlarınız, yazdığınız metinler; her bir cam pencerenin ekranda tam olarak nerede (X, Y koordinatı olarak) asılı kaldığı bilgisiyle birlikte eş zamanlı olarak konumunuzdaki `%AppData%\DockShelf\config.json` dosyasına işlenir. Uygulamayı tamamen kapatıp tekrar başlatsanız dahi verileriniz aynı bıraktığınız düzende yüklenir.
3. **Sabitleme / Raptiye İkonu (Pin):** Artık her rafın köşesinde minik bir `📌` butonu var.
   - İkon tam beyaz (aktif) durumdayken pencereniz "Her Zaman Üstte" kalarak MacOS doklarındaki gibi daima göz önünde bulunur.
   - İkona basıp kapattığınızda ikon şeffaflaşır. Pencere, tıpkı standart masaüstü simgeleriniz gibi davranarak açık olan diğer klasör veya programlarınızın altında usulca gizlenir.
4. **%50 Küçültülmüş Ekran:** Arayüz bileşenleri (simge büyüklükleri, yazılar, iç - dış boşlukluklar) ilk plana göre matematiksel olarak tam yarı yarıya küçültüldü. Artık ekranınızda gereksiz yer kaplamayan tamamen derli toplu ve estetik bir minimalizm sağlanıyor.
