# Windows Dock/Shelf Application Plan (WPF)

The goal is to build a Windows desktop application that mimics a dynamic "Dock" or "Shelf" for files, links, and text snippets. The user should be able to drag and drop text/links/executables, run files, and create multiple docks.

**Update:** Based on your feedback, we will use **WPF (Windows Presentation Foundation) with C#** instead of Electron. This is fundamentally correct: Electron Chromium tab consumes more memory, whereas WPF is natively compiled for Windows, starts significantly faster, takes minimal RAM, and is perfect for background tools.

## Proposed Architecture & Stack

- **Core Desktop Framework:** WPF on .NET 8 (C#)
  - Provides native access to Windows shell APIs (perfect for extracting `.exe` icons and sending text to Notepad).
  - EXTREMELY lightweight resource usage compared to Electron.
- **UI Design:** 
  - Using XAML to create frameless, background-transparent windows with rounded corners.
  - Using `System.Windows.Shell.WindowChrome` or raw Win32 APIs (via `DwmEnableBlurBehindWindow`) for premium glass (Acrylic) effects on Windows 10/11.

## Key Features Implementation

### 1. Frameless Acrylic Window
- We will set the WPF Window to `WindowStyle="None"` and `AllowsTransparency="True"`.
- We can implement Windows 11 Acrylic/Mica backdrop effects using native invocations to give it that "premium MacOS shelf" look on Windows.

### 2. Drag & Drop functionality
- Using WPF's native `AllowDrop="True"` and `Drop` event handlers.
- **Text/Links:** Grabbed via `e.Data.GetData(DataFormats.Text)`.
- **Files (.exe):** Grabbed via `e.Data.GetData(DataFormats.FileDrop)`. We can dynamically extract the real application icon using `System.Drawing.Icon.ExtractAssociatedIcon` to display it on the shelf.

### 3. File Execution & Notepad Integration
- Executables are run via C#'s `System.Diagnostics.Process.Start(filepath)`.
- Text droplets: When clicked, we save the text payload to a temporary `.txt` file and run `Process.Start("notepad.exe", tempFilePath)`.

### 4. Multiple Docks (Shelves)
- The main application will have a `DockManager` that can spawn new WPF `<Window>` instances upon request.
- We will save the items of all shelves to a local `config.json` file in your `AppData` so they persist through PC reboots.
