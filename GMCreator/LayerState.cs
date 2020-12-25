using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace GMCreator
{
    class LayerStateManager
    {
        // state for which layers to render
        private Layers state;
        private Dictionary<Layers, ToolStripMenuItem> menuItemLayer;

        // need to figure out a way to make toggles in the app reflect in the menu options
        [Flags]
        internal enum Layers
        {
            None = 0,
            Background = 1,
            Foreground = 2,
            Boxes = 4,
            BoxContents = 8,
            All = BoxContents | Boxes | Foreground | Background,
            Invalid = Int32.MaxValue & ~All
        }

        public LayerStateManager(ToolStripMenuItem[] items)
        {
            menuItemLayer = new Dictionary<Layers, ToolStripMenuItem>();
            Layers left = state = Layers.All;
            int i = 0;
            for(Layers layer = Layers.Background; left != Layers.None; layer = (Layers)((int)layer << 1), ++i)
            {
                ToolStripMenuItem item = items[i];
                item.Tag = layer;
                item.Checked = true;
                menuItemLayer.Add(layer, item);
                left &= ~layer;
            }
        }

        public Layers Toggle(Layers which)
        {
            Debug.Assert((which & Layers.Invalid) == 0);
            Layers single = Layers.Background;
            while(which != Layers.None)
            {
                if ((single & which) != 0)
                {
                    state ^= single;
                    menuItemLayer[single].Checked = !menuItemLayer[single].Checked;
                    Debug.Assert(menuItemLayer[single].Checked == ((state & single) != 0));
                    which &= ~single;
                }
                single = (Layers)((int)single << 1);
            }
            return state;
        }

        public Layers On(Layers which)
        {
            Debug.Assert((which & Layers.Invalid) == 0);
            Layers single = Layers.Background;
            while (which != Layers.None)
            {
                if ((single & which) != 0)
                {
                    state |= single;
                    menuItemLayer[single].Checked = true;
                    which &= ~single;
                }
                single = (Layers)((int)single << 1);
            }
            return state;
        }

        public Layers Off(Layers which)
        {
            Debug.Assert((which & Layers.Invalid) == 0);
            Layers single = Layers.Background;
            while (which != Layers.None)
            {
                if ((single & which) != 0)
                {
                    state &= ~single;
                    menuItemLayer[single].Checked = false;
                    which &= ~single;
                }
                single = (Layers)((int)single << 1);
            }
            return state;
        }

        public Layers Query(Layers which)
        {
            return state & which;
        }
    }
}
