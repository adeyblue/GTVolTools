using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace GMCreator
{
    public abstract class IBox
    {
        protected const int ResizePointTopBit = 0x1;
        protected const int ResizePointBottomBit = 0x2;
        protected const int ResizePointLeftBit = 0x4;
        protected const int ResizePointRightBit = 0x8;
        public enum BoxHitTest
        {
            None = 0, // 
            AnchorPoint, // central point where a click and hold will drag
            ResizePointTL = ResizePointTopBit | ResizePointLeftBit, // where a click and drag will resize the box. TopLeft-BottomRight corners
            ResizePointTR = ResizePointTopBit | ResizePointRightBit, // where a click and drag will resize the box. TopRight-BottomLeft corners
            ResizePointBL = ResizePointBottomBit | ResizePointLeftBit, // where a click and drag will resize the box. TopRight-BottomLeft corners
            ResizePointBR = ResizePointBottomBit | ResizePointRightBit // where a click and drag will resize the box. TopLeft-BottomRight corners
        }

        abstract public string Name
        {
            get;
            set;
        }

        abstract public void Serialize(System.IO.BinaryWriter stream);
        abstract public void Draw(Graphics g, Rectangle r);
        abstract public IBox Clone();
        abstract public void Move(Point delta);
        abstract public void Resize(BoxHitTest corner, Point delta);
        abstract public BoxHitTest HitTest(Point p);
        abstract public void Select(bool state);

        protected void RaiseDescriptionChanged()
        {
            if (DescriptionChanged != null)
            {
                DescriptionChanged(this, EventArgs.Empty);
            }
        }

        protected void RaiseDisplayChanged(Rectangle oldRect, Rectangle newRect)
        {
            if (DisplayChanged != null)
            {
                BoxDisplayChangeEventArgs e = new BoxDisplayChangeEventArgs(oldRect, newRect);
                DisplayChanged(this, e);
            }
        }

        public event DescriptionDisplayChange DescriptionChanged;
        public event BoxDisplayChange DisplayChanged;
    }

    public class InvalidBoxStateException : Exception
    {
        public InvalidBoxStateException(string message, IBox box)
            : base(message)
        {
            InvalidBox = box;
        }

        public IBox InvalidBox
        {
            get;
            private set;
        }
    }

    public class BoxDisplayChangeEventArgs : EventArgs
    {
        public Rectangle InvalidatedArea
        {
            get;
            private set;
        }

        public BoxDisplayChangeEventArgs(Rectangle oldRectangle, Rectangle newRectangle)
        {
            InvalidatedArea = Rectangle.Union(oldRectangle, newRectangle);
        }
    }

    public delegate void BoxDisplayChange(object sender, BoxDisplayChangeEventArgs e);
    public delegate void DescriptionDisplayChange(object sender, EventArgs e);
    public delegate void PropertyChange(object sender, EventArgs e);
}
