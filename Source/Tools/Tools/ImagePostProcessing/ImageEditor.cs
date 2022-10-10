using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GraphicMinimal;
using ImageCreator;
using System.IO;
using BitmapHelper;
using System.Drawing.Imaging;

namespace Tools.Tools.ImagePostProcessing
{
    public partial class ImageEditor : Form
    {
        private ImageBuffer image = null;
        private Bitmap fireMaskFromUser = null;

        public ImageEditor()
        {
            InitializeComponent();
            
            this.comboBox1.Items.AddRange(Enum.GetNames(typeof(TonemappingMethod)));
            this.comboBox1.SelectedIndex = 0;
            this.panel2.UpperBoundGrayChanged += (sender, value) => { Reload(); };
            this.checkBox1.Checked = this.panel2.ShowClampedValuesRed;
        }

        //Open Image Button
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;

                this.image = ImagePostProcessingHelper.ReadImageBufferFromFile(openFileDialog1.FileName);

                this.panel2.UpdateImage(image);
                this.checkBox1.Checked = true;
                this.numericUpDown1.Value = 80;
                Reload();
            }
        }

        private void Reload()
        {
            if (this.image != null)
            {
                this.panel1.BackgroundImage = GetBitmapWithAllEffects(GetImageBufferWithAllEffects());

                this.panel1.Invalidate();
                this.panel2.Invalidate();
                this.trackbarFloat1.Invalidate();
                this.trackbarFloat2.Invalidate();
                this.trackbarFloat3.Invalidate();
            }
        }

        private ImageBuffer GetImageBufferWithAllEffects()
        {
            var image1 = this.image;
            if (this.checkBox2.Checked)
            {
                image1 = image.RemoveFireFlys(this.fireMaskFromUser);
            }
            if (this.trackbarFloat1.ScrollValue != 1)
            {
                image1 = image1.GetColorScaledImage(this.trackbarFloat1.ScrollValue);
            }
            if (this.trackbarFloat2.ScrollValue != 1)
            {
                image1 = image1.GetGammaCorrectedImage(this.trackbarFloat2.ScrollValue);
            }
            

            image1 = this.panel2.GetClampedImageBuffer(image1);

            return image1;
        }

        private Bitmap GetBitmapWithAllEffects(ImageBuffer image)
        {
            Bitmap bitmap = Tonemapping.GetImage(image, (TonemappingMethod)Enum.Parse(typeof(TonemappingMethod), this.comboBox1.Text));

            if (this.trackbarFloat3.ScrollValue != 1)
            {
                bitmap = BitmapHelp.ScaleInHSLSpace(bitmap, 1, this.trackbarFloat3.ScrollValue, 1);
            }

            return bitmap;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            if (this.BackgroundImage != null)
            {
                e.Graphics.DrawImage(this.BackgroundImage, new Point(0, 0));
            }
        }

        //Tonemapping-Combobox
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Reload();
        }

        //Create all Tonemapping-Images
        private void button2_Click(object sender, EventArgs e)
        {
            CreateMasterImage().Save("TonemappingResult.bmp");
            MessageBox.Show(Directory.GetCurrentDirectory() + "\\TonemappingResult.bmp created");
        }

        private Bitmap CreateMasterImage()
        {
            int header = 20;
            var enumValues = Enum.GetNames(typeof(TonemappingMethod)).Select(x => (TonemappingMethod)Enum.Parse(typeof(TonemappingMethod), x)).ToList();
            Bitmap resultImage = new Bitmap(this.image.Width * enumValues.Count, this.image.Height + header);
            Graphics grx = Graphics.FromImage(resultImage);

            for (int i = 0; i < enumValues.Count; i++)
            {
                grx.DrawImage(Tonemapping.GetImage(this.image, enumValues[i]), new Point(i * this.image.Width, header));
                grx.DrawString(enumValues[i].ToString(), new Font("Arial", 10), Brushes.Black, new Point(i * this.image.Width, 0));
            }

            grx.Dispose();

            return resultImage;
        }

        //Histogram-Show-Red-Color-Checkbox
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.panel2.ShowClampedValuesRed = this.checkBox1.Checked;
            Reload();
        }

        //Histogram-Color-Clamping
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            this.panel2.ScaleFactorForClampedColors = (float)this.numericUpDown1.Value / 100;
            Reload();
        }

        //Save Button
        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "bmp files (*.bmp)|*.bmp|jpg files (*.jpg)|*.jpg|Hdr files (*.hdr)|*.hdr|Raw Imagedata (*.raw)|*.raw|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (saveFileDialog1.FileName.EndsWith(".hdr"))
                {
                    new RgbeFile(this.image).WriteToFile(saveFileDialog1.FileName);
                }else if (saveFileDialog1.FileName.EndsWith(".raw"))
                {
                    GetImageBufferWithAllEffects().WriteToFile(saveFileDialog1.FileName);
                }
                else if (saveFileDialog1.FileName.EndsWith(".jpg"))
                {
                    this.panel1.BackgroundImage.Save(saveFileDialog1.FileName, ImageFormat.Jpeg);
                }
                else
                {
                    this.panel1.BackgroundImage.Save(saveFileDialog1.FileName);
                }
                
            }
        }

        //Remove Fireflys Checkbox
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Reload();
        }

        //Open FireFly-Mask
        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = openFileDialog1.FileName;
                this.fireMaskFromUser = new Bitmap(openFileDialog1.FileName);               
            }
        }

        //Scale-Down-Button
        private void button5_Click(object sender, EventArgs e)
        {
            NumberSelector form = new NumberSelector(new SelectorItem[] {
                new SelectorItem("2", () => ScaleImageDown(2) ),
                new SelectorItem("3", () => ScaleImageDown(3) ),
                new SelectorItem("4", () => ScaleImageDown(4) ),
                new SelectorItem("5", () => ScaleImageDown(5) ),
            });

            form.StartPosition = FormStartPosition.Manual;
            var b = (sender as Button);
            var p = new Point(this.Location.X + b.Location.X + b.Width / 2 - form.Width / 2, this.Location.Y + b.Location.Y + b.Height / 2 + form.Height / 2);        
            form.Location = p;

            form.Show();
        }

        private void ScaleImageDown(int scaleSize)
        {
            this.image = this.image.ScaleSizeDown(scaleSize, false);
            Reload();
        }

        //Helligkeit-Scroll-Callback
        private void trackbarFloat1_MyOnScrollHandler(float newScrollValue)
        {
            Reload();
        }

        //Gamma-Scroll-Callback
        private void trackbarFloat2_MyOnScrollHandler(float newScrollValue)
        {
            Reload();
        }

        //Sättigung-Scoll-Callback
        private void trackbarFloat3_MyOnScrollHandler(float newScrollValue)
        {
            Reload();
        }

        
    }

    

    
}
