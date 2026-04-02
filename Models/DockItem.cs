using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace DockShelf.Models
{
    public enum DockItemType
    {
        Executable,
        File,
        Link,
        TextSnippet,
        Folder
    }

    public class DockItem : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private string _filePath;
        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        private DockItemType _type;
        public DockItemType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        private ImageSource _iconImage;
        [System.Text.Json.Serialization.JsonIgnore]
        public ImageSource IconImage
        {
            get => _iconImage;
            set { _iconImage = value; OnPropertyChanged(); }
        }

        private string _textContent;
        // Used when type is TextSnippet
        public string TextContent
        {
            get => _textContent;
            set { _textContent = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
