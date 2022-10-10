using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tools.Tools.ImagePostProcessing
{
    public partial class NumberSelector : Form
    {
        public NumberSelector() 
        {
            InitializeComponent();
        }

        public NumberSelector(SelectorItem[] items)
        {
            InitializeComponent();

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.BackColor = this.flowLayoutPanel1.BackColor = Color.LimeGreen;
            this.TransparencyKey = Color.LimeGreen;

            foreach (var item in items)
            {
                this.flowLayoutPanel1.Controls.Add(CreateButton(item));
            }

            this.Width = items.Length * this.flowLayoutPanel1.Controls[0].Width;
            this.Height = this.flowLayoutPanel1.Controls[0].Height;
        }

        private Button CreateButton(SelectorItem item)
        {
            Button button = new Button();

            button.Location = new System.Drawing.Point(3, 3);
            button.Name = "button1";
            button.Size = new System.Drawing.Size(25, 25);
            button.Text = item.Text;
            button.UseVisualStyleBackColor = true;
            button.Padding = new System.Windows.Forms.Padding(0);
            button.Margin = new System.Windows.Forms.Padding(0);

            button.BackColor = Color.LightSkyBlue;
            button.ForeColor = Color.Black;
            button.Font = new Font(button.Font, FontStyle.Bold);

            button.Click += (sender, e) =>
            {
                this.Close();
                Task.Run(item.CallBack);
            };

            return button;
        }

        private void NumberSelector_Deactivate(object sender, EventArgs e)
        {
            this.Close();
        }
    }

    public class SelectorItem
    {
        public string Text { get; private set; }
        public Action CallBack { get; private set; }

        public SelectorItem(string text, Action callBack)
        {
            this.Text = text;
            this.CallBack = callBack;
        }
    }
}
