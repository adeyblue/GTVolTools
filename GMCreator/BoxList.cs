using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Text;

namespace GMCreator
{
    class BoxList
    {
        public BoxList()
        {
            Items = new BindingList<IBox>();
        }

        public void Draw(Graphics g, Rectangle clipRect, Rectangle currentlyDrawing)
        {
            foreach (IBox b in Items)
            {
                b.Draw(g, clipRect);
            }
            if (currentlyDrawing != Rectangle.Empty)
            {
                g.DrawRectangle(Globals.App.DefaultOutlinePen, currentlyDrawing);
            }
        }

        public Box AddNewBox(Rectangle loc)
        {
            Box b = new Box(loc);
            Add(b);
            return b;
        }

        public void Add(IBox b)
        {
            b.DescriptionChanged += new DescriptionDisplayChange(OnBoxDescriptionChange);
            Items.Add(b);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public BindingList<IBox> Items
        {
            get;
            private set;
        }

        public IBox.BoxHitTest HitTest(Point p, ref IBox hitBox)
        {
            IBox.BoxHitTest res = IBox.BoxHitTest.None;
            foreach (IBox item in Items)
            {
                if ((res = item.HitTest(p)) != IBox.BoxHitTest.None)
                {
                    hitBox = item;
                    break;
                }
            }
            return res;
        }

        private void OnBoxDescriptionChange(object sender, EventArgs e)
        {
            BindingList<IBox> list = Items;
            IBox box = (IBox)sender;
            int boxIdx = list.IndexOf(box);
            Debug.Assert(boxIdx != -1);
            list.ResetItem(boxIdx);
        }

        public void Remove(IBox box)
        {
            Items.Remove(box);
        }

        public void Load(List<IBox> boxes)
        {
            foreach (IBox ib in boxes)
            {
                Add(ib);
            }
        }

        public void SetInnerRender(bool value)
        {
            foreach (IBox b in Items)
            {
                Box box = b as Box;
                if (box != null)
                {
                    box.ShowInnerContent = value;
                }
            }
        }

        public IBox this[int index]
        {
            get
            {
                return Items[index];
            }
        }

        public int Count
        {
            get
            {
                return Items.Count;
            }
        }
    }
}
