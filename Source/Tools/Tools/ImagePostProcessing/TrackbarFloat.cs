using System.Drawing;
using System.Windows.Forms;

namespace Tools.Tools.ImagePostProcessing
{
    public partial class TrackbarFloat : UserControl
    {
        private float scroll = 0.5f;
        private float min = 0;
        private float max = 100;
        private Rectangle posRec = new Rectangle(0, 0, 1, 1);
        private bool scroll_click = false;
        private float startValue = float.NaN;

        public delegate void MyOnScroll(float newScrollValue);
        public event MyOnScroll MyOnScrollHandler = null;

        public float ScrollValue
        {
            get { return scroll; }
            set
            {
                if (float.IsNaN(this.startValue)) this.startValue = value;
                if (value < min) value = min;
                if (value > max) value = max;
                scroll = value;
                this.toolTip1.SetToolTip(this, value.ToString());
                if (MyOnScrollHandler != null) MyOnScrollHandler(scroll); 
                this.Invalidate();
            }
        }
        public float MinValue
        {
            get { return min; }
            set { if (value < max) min = value; }
        }
        public float MaxValue
        {
            get { return max; }
            set { if (value > min) max = value; }
        }

        public TrackbarFloat()
        {
            InitializeComponent();

            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MyTrackbar_MouseMove);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MyTrackbar_MouseClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MyTrackbar_MouseDown);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MyTrackbar_MouseUp);
            this.MouseDoubleClick += MyTrackbarFloat_MouseDoubleClick;
        }

        private void MyTrackbarFloat_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.ScrollValue = this.startValue;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.DrawLine(new Pen(Brushes.Black, 3), 0, this.Height / 2, this.Width, this.Height / 2);
            float x = (scroll - min) * this.Width / (max - min);
            posRec = new Rectangle((int)(x - 4), 0, 8, this.Height);
            e.Graphics.FillRectangle(Brushes.Blue, posRec);
        }

        private void MyTrackbar_MouseDown(object sender, MouseEventArgs e)
        {
            if (!scroll_click && e.X >= posRec.Left && e.X <= posRec.Right && e.Y >= posRec.Top && e.Y <= posRec.Bottom) scroll_click = true;
        }

        private void MyTrackbar_MouseUp(object sender, MouseEventArgs e)
        {
            scroll_click = false;
        }

        private void MyTrackbar_MouseMove(object sender, MouseEventArgs e)
        {
            if (scroll_click)
            {
                ScrollValue = (float)e.X * (max - min) / (float)this.Width + min;
            }
        }

        private void MyTrackbar_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.X < posRec.Left) ScrollValue -= (max - min) / 10;
            if (e.X > posRec.Right) ScrollValue += (max - min) / 10;
        }
    }
}
