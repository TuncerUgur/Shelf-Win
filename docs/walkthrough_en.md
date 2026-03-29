# DockShelf (WPF) - Walkthrough

The development of the Windows Dock application is complete. Below is a summary of what was accomplished, the key features, and how it was implemented.

## What Was Accomplished

1. **Lightweight WPF Project Initialization:** We created a brand-new WPF (C# / .NET 8) project named `DockShelf`. This ensures the application consumes barely any RAM and runs natively compared to Chromium-based Electron applications.
2. **Glassmorphism UI Design:** Using XAML borders, dropshadow effects, and transparent windows, we created a floating "macOS-like" shelf. The UI seamlessly stays on top of other windows and doesn't pollute the Windows Taskbar (`ShowInTaskbar="False"`).
3. **Advanced Drag & Drop System:**
   - Evaluates whether the dropped payload is a `FileDrop` or plain `Text`.
   - Native executables (`.exe`) have their app icons automatically extracted using `System.Drawing.Icon.ExtractAssociatedIcon` to instantly show up on the dock as a clickable shortcut.
   - Text drops are truncated and shown as "Txt" bubbles.
4. **Execution & Notepad Integration:**
   - Single-clicking a file or `.exe` automatically spawns it through the operating system (`Process.Start`).
   - Single-clicking text extracts the payload to a local temporary text file and opens it explicitly with `notepad.exe`.

## Technical Summary
- **Files Modified/Created:**
  - `DockShelf.csproj`: Configuration updated to support System.Windows.Forms for icon extraction.
  - `App.xaml.cs`: Ensured correct `Application` reference limits.
  - `MainWindow.xaml`: XAML UI definitions for dock aesthetics.
  - `MainWindow.xaml.cs`: Heavy lifting for drag and drop events, window dragging capabilities, and Process executions.
  - `Models/DockItem.cs`: Structural definition representing individual docked items.

> [!TIP]
> **Performance Improvements:** By choosing WPF over our initial Electron idea, background execution RAM was cut down significantly and Windows-native drag handlers perform instantly without inter-process communication overheads.

## Verification

The project compiles correctly and natively binds to the Windows `.exe` registry and native desktop APIs.
To verify manually, you can navigate to your folder and type:
```powershell
dotnet run
```
And start dropping `.exe` items from your desktop directly into the glass shelf!
