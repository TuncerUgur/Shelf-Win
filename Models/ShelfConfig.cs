using System;
using System.Collections.Generic;

namespace DockShelf.Models
{
    public class ShelfConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ShelfName { get; set; } = "";

        public double Left { get; set; }
        public double Top { get; set; }
        public bool IsPinned { get; set; } = true;
        public bool IsVertical { get; set; } = false;
        public bool IsLocked { get; set; } = false;
        
        // Phase 4 settings
        public int MatrixRows { get; set; } = 0; // 0 = auto
        public int MatrixColumns { get; set; } = 0; // 0 = auto
        public string BackgroundImagePath { get; set; } = null;

        public List<DockItem> Items { get; set; } = new List<DockItem>();
    }
}
