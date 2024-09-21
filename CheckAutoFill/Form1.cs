//using Emgu.CV.CvEnum;
//using Emgu.CV;
//using System.Runtime.InteropServices;
//using System.Collections;
//using System.Drawing.Text;
//using System.Drawing;
//using System.DirectoryServices;
//using System.Diagnostics;
//using Emgu.CV.OCR;
//using System;
//using Microsoft.Office.Interop.Word;

using static Emgu.CV.VideoCapture;

namespace CheckAutoFill
{
    public partial class Form1 : Form
    {
        System.Threading.Thread _thread1;
        System.Threading.Thread _thread2;
        System.Drawing.Bitmap _bitmap1;
        System.Drawing.Bitmap _bitmapROI;
        bool _IsBMPReady = false;
        bool _flipHorizontal = false;
        bool _flipVertical = false;
        bool _thread1Continue = false;
        bool _thread2Continue = false;
        System.String _OCROutput = "";
        System.String _OCRError = "";

        public Form1()
        {
            InitializeComponent();
        }
        private void CaptureFrame()
        {
            Emgu.CV.Mat mat1 = new Emgu.CV.Mat();
            Emgu.CV.VideoCapture videoCapture1 = new Emgu.CV.VideoCapture();

            while (_thread1Continue)
            {
                videoCapture1.Read(mat1);

                if (!_flipVertical & !_flipHorizontal)
                {
                    //do nothing
                }
                else if (_flipVertical & !_flipHorizontal)
                {
                    Emgu.CV.Mat mat2 = mat1.Clone();
                    Emgu.CV.CvInvoke.Flip(mat2, mat1, Emgu.CV.CvEnum.FlipType.Vertical);
                }
                else if (!_flipVertical & _flipHorizontal)
                {
                    Emgu.CV.Mat mat2 = mat1.Clone();
                    Emgu.CV.CvInvoke.Flip(mat2, mat1, Emgu.CV.CvEnum.FlipType.Horizontal);
                }
                else if (_flipVertical & _flipHorizontal)
                {
                    Emgu.CV.Mat mat2 = mat1.Clone();
                    Emgu.CV.CvInvoke.Flip(mat2, mat1, Emgu.CV.CvEnum.FlipType.Both);
                }

                // convert Emgu.CV.Mat to byte array
                byte[] byteArray1 = new byte[mat1.Width * mat1.Height * mat1.ElementSize];
                System.Runtime.InteropServices.Marshal.Copy(mat1.DataPointer, byteArray1, 0, byteArray1.Length);

                // create a bmp from the byte array
                System.Runtime.InteropServices.GCHandle pinnedArray = System.Runtime.InteropServices.GCHandle.Alloc(byteArray1, System.Runtime.InteropServices.GCHandleType.Pinned);
                IntPtr ptr = pinnedArray.AddrOfPinnedObject();
                _bitmap1 = new System.Drawing.Bitmap(mat1.Width, mat1.Height, 3 * mat1.Width, System.Drawing.Imaging.PixelFormat.Format24bppRgb, ptr);
                pinnedArray.Free();

                _IsBMPReady = true;
            }
            _IsBMPReady = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            _penArray[0] = System.Drawing.Pens.Red;
            _penArray[1] = System.Drawing.Pens.Green;

            button2.Enabled = false;
            button3.Enabled = false;
            button5.Enabled = false;
        }

        private System.String MatToString(Emgu.CV.Mat mat)
        {
            System.String str = "";
            str = str + "Cols=" + mat.Cols.ToString() + "\n";
            str = str + "Depth=" + mat.Depth.ToString() + "\n";
            str = str + "Dims=" + mat.Dims.ToString() + "\n";
            str = str + "ElementSize=" + mat.ElementSize.ToString() + "\n";
            str = str + "Height=" + mat.Height.ToString() + "\n";
            str = str + "IsContinuous=" + mat.IsContinuous.ToString() + "\n";
            str = str + "IsEmpty=" + mat.IsEmpty.ToString() + "\n";
            str = str + "IsSubMatrix=" + mat.IsSubmatrix.ToString() + "\n";
            str = str + "NumberOfChannels=" + mat.NumberOfChannels.ToString() + "\n";
            str = str + "Rows=" + mat.Rows.ToString() + "\n";
            str = str + "Size=" + mat.Size.ToString() + "\n";
            str = str + "SizeOfDimension=" + mat.SizeOfDimension.ToString() + "\n";
            str = str + "Step=" + mat.Step.ToString() + "\n";
            str = str + "Total=" + mat.Total.ToString() + "\n";
            str = str + "Width=" + mat.Width.ToString() + "\n";
            return (str);
        }

        private System.Drawing.Point _p1 = System.Drawing.Point.Empty;
        private bool _isMouseDown = false;
        private int _rectToggle = 0;
        //private System.Drawing.Rectangle _prevRect;
        private System.Drawing.Rectangle[] _rectArray = new System.Drawing.Rectangle[2];
        private System.Drawing.Pen[] _penArray = new System.Drawing.Pen[2];
        //private System.Drawing.Rectangle _prevImageRect;
        System.Drawing.Rectangle _RectIm = new System.Drawing.Rectangle();
        private bool _didOnce = false;
        private bool _drawable = false;

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            _p1 = new System.Drawing.Point(e.X, e.Y);
            _isMouseDown = true;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            _isMouseDown = false;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMouseDown)
            {
                return;
            }

            System.Drawing.Point p2 = new System.Drawing.Point(e.X, e.Y);
            System.Drawing.Rectangle rect1 = new System.Drawing.Rectangle(System.Math.Min(_p1.X, p2.X), System.Math.Min(_p1.Y, p2.Y), System.Math.Abs(_p1.X - p2.X), System.Math.Abs(_p1.Y - p2.Y));

            _rectArray[_rectToggle] = rect1;
            button3.Enabled = true;

            pictureBox1.Invalidate();

        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (!_drawable)
            {
                return;
            }

            for (int i = 0; i < _rectArray.Length; i++)
            {
                if (!_rectArray[i].IsEmpty)
                {
                    e.Graphics.DrawRectangle(_penArray[i], _rectArray[i]);
                }
            }
            //e.Graphics.DrawRectangle(System.Drawing.Pens.Magenta, _RectIm);
        }

        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                return;
            }

            System.Drawing.Rectangle rectIm = new System.Drawing.Rectangle();

            double scaleX = System.Convert.ToDouble(pictureBox1.ClientSize.Width) / System.Convert.ToDouble(pictureBox1.Image.Width);
            double scaleY = System.Convert.ToDouble(pictureBox1.ClientSize.Height) / System.Convert.ToDouble(pictureBox1.Image.Height);
            double scale1 = System.Math.Min(scaleX, scaleY);
            rectIm.Width = System.Convert.ToInt32(pictureBox1.Image.Width * scale1);
            rectIm.Height = System.Convert.ToInt32(pictureBox1.Image.Height * scale1);
            rectIm.X = (pictureBox1.Width - rectIm.Width) / 2;
            rectIm.Y = (pictureBox1.Height - rectIm.Height) / 2;

            //double hScale = System.Convert.ToDouble(rect1.Width) / System.Convert.ToDouble(_prevImageRect.Width);
            //double vScale = System.Convert.ToDouble(rect1.Height) / System.Convert.ToDouble(_prevImageRect.Height);

            double scale2 = System.Convert.ToDouble(rectIm.Width) / System.Convert.ToDouble(_RectIm.Width);

            for (int i = 0; i < _rectArray.Length; i++)
            {
                if (!_rectArray[i].IsEmpty)
                {
                    int X1 = _rectArray[i].X - _RectIm.X;
                    int Y1 = _rectArray[i].Y - _RectIm.Y;
                    X1 = System.Convert.ToInt32(X1 * scale2);
                    Y1 = System.Convert.ToInt32(Y1 * scale2);
                    _rectArray[i].Width = System.Convert.ToInt32(_rectArray[i].Width * scale2);
                    _rectArray[i].Height = System.Convert.ToInt32(_rectArray[i].Height * scale2);
                    _rectArray[i].X = rectIm.X + X1;
                    _rectArray[i].Y = rectIm.Y + Y1;

                }
            }

            _RectIm = rectIm;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            _thread1Continue = false;
            button1.Text = "Start";
            button2.Enabled = false;
            button3.Enabled = true;
            button4.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (_IsBMPReady)
            {
                pictureBox1.Image = (System.Drawing.Image)_bitmap1;

                if (!_rectArray[0].IsEmpty)
                {
                    double scale1 = System.Convert.ToDouble(_bitmap1.Width) / System.Convert.ToDouble(_RectIm.Width);

                    int X1 = _rectArray[0].X - _RectIm.X;
                    int Y1 = _rectArray[0].Y - _RectIm.Y;
                    X1 = System.Convert.ToInt32(X1 * scale1);
                    Y1 = System.Convert.ToInt32(Y1 * scale1);

                    System.Drawing.Rectangle rect1 = new System.Drawing.Rectangle(X1, Y1, System.Convert.ToInt32(_rectArray[0].Width * scale1), System.Convert.ToInt32(_rectArray[0].Height * scale1));

                    // crop image
                    _bitmapROI = _bitmap1.Clone(rect1, _bitmap1.PixelFormat);
                }

                if (!_didOnce)
                {
                    double scaleX = System.Convert.ToDouble(pictureBox1.ClientSize.Width) / System.Convert.ToDouble(pictureBox1.Image.Width);
                    double scaleY = System.Convert.ToDouble(pictureBox1.ClientSize.Height) / System.Convert.ToDouble(pictureBox1.Image.Height);
                    double scale1 = System.Math.Min(scaleX, scaleY);
                    _RectIm.Width = System.Convert.ToInt32(pictureBox1.Image.Width * scale1);
                    _RectIm.Height = System.Convert.ToInt32(pictureBox1.Image.Height * scale1);
                    _RectIm.X = (pictureBox1.Width - _RectIm.Width) / 2;
                    _RectIm.Y = (pictureBox1.Height - _RectIm.Height) / 2;
                    _didOnce = true;
                    button5.Enabled = true;
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _flipHorizontal = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            _flipVertical = checkBox2.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Start")
            {
                System.Threading.ThreadStart threadDelegate = new System.Threading.ThreadStart(CaptureFrame);
                _thread1 = new System.Threading.Thread(threadDelegate);
                _thread1Continue = true;
                _thread1.Start();

                timer1.Interval = 100;
                _didOnce = false;
                timer1.Enabled = true;

                button1.Text = "Stop";
                button2.Enabled = true;
                _drawable = true;
                button4.Enabled = false;
            }
            else if (button1.Text == "Stop")
            {
                _thread1Continue = false;
                timer1.Enabled = false;
                pictureBox1.Image = null;

                button1.Text = "Start";
                button2.Enabled = false;
                button3.Enabled = false;
                _drawable = false;
                button4.Enabled = true;
                button5.Enabled = false;
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            double scale1 = System.Convert.ToDouble(_bitmap1.Width) / System.Convert.ToDouble(_RectIm.Width);

            int X1 = _rectArray[0].X - _RectIm.X;
            int Y1 = _rectArray[0].Y - _RectIm.Y;
            X1 = System.Convert.ToInt32(X1 * scale1);
            Y1 = System.Convert.ToInt32(Y1 * scale1);

            System.Drawing.Rectangle rect1 = new System.Drawing.Rectangle(X1, Y1, System.Convert.ToInt32(_rectArray[0].Width * scale1), System.Convert.ToInt32(_rectArray[0].Height * scale1));

            // crop image
            _bitmapROI = _bitmap1.Clone(rect1, _bitmap1.PixelFormat);

            _drawable = false;
            for (int i = 0; i < _rectArray.Length; i++)
            {
                _rectArray[i] = System.Drawing.Rectangle.Empty;
            }

            pictureBox1.Image = _bitmapROI;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                System.Threading.ThreadStart threadDelegate = new System.Threading.ThreadStart(OCR);
                _thread2 = new System.Threading.Thread(threadDelegate);
                _thread2Continue = true;
                _thread2.Start();
                timer2.Interval = 1000;
                timer2.Enabled = true;
            }
            else
            {
                _thread2Continue = false;
                timer2.Enabled = false;
                label1.Text = "";
            }

        }

        private void OCR()
        {
            while (_thread2Continue)
            {
                System.Diagnostics.Process process1 = new System.Diagnostics.Process();

                process1.StartInfo.FileName = "C:\\Users\\uture\\source\\repos\\CheckAutoFill\\Tesseract-OCR\\" + "tesseract.exe";
                process1.StartInfo.Arguments = "stdin" + " stdout -l " + "eng";
                process1.StartInfo.UseShellExecute = false;
                process1.StartInfo.RedirectStandardInput = true;
                process1.StartInfo.RedirectStandardOutput = true;
                process1.StartInfo.RedirectStandardError = true;
                process1.StartInfo.CreateNoWindow = true;
                process1.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

                process1.Start();

                System.IO.BinaryWriter writer1 = new System.IO.BinaryWriter(process1.StandardInput.BaseStream);
                _bitmapROI.Save(writer1.BaseStream, System.Drawing.Imaging.ImageFormat.Bmp);
                process1.StandardInput.BaseStream.Close();

                System.IO.StreamReader reader1 = process1.StandardOutput;
                _OCROutput = reader1.ReadToEnd();

                System.IO.StreamReader reader2 = process1.StandardError;
                _OCRError = reader2.ReadToEnd();

                process1.WaitForExit();
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            label1.Text = _OCROutput;
            if (_OCRError != null)
            {
                if (_OCRError != "")
                {
                    richTextBox1.AppendText(_OCRError + "\n");
                }
            }

        }

        private void AddToLog(System.String str)
        {
            richTextBox1.AppendText("<" + str + ">" + "\n");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            openFileDialog1.Multiselect = false;
            System.Windows.Forms.DialogResult dialogResult1 = openFileDialog1.ShowDialog();
            if (dialogResult1 == DialogResult.OK)
            {
                _bitmap1 = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(openFileDialog1.FileName);
                pictureBox1.Image = _bitmap1;
                _drawable = true;
                button5.Enabled = true;

            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            System.Windows.Forms.DialogResult dialogResult1 = saveFileDialog1.ShowDialog();
            if (dialogResult1 == DialogResult.OK)
            {
                _bitmap1.Save(saveFileDialog1.FileName);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {

            Microsoft.Office.Interop.Word.Application application1 = new Microsoft.Office.Interop.Word.Application();
            application1.AutomationSecurity = Microsoft.Office.Core.MsoAutomationSecurity.msoAutomationSecurityLow;
            application1.Visible = true;

            Microsoft.Office.Interop.Word.Document document1 = new Microsoft.Office.Interop.Word.Document();
            document1 = application1.Documents.Open("C:\\Users\\uture\\source\\repos\\CheckAutoFill\\check.docm");

            AddToLog(document1.Name);

            Microsoft.Office.Interop.Word.Shapes shapes1 = document1.Shapes;
            for (int i = 1; i <= shapes1.Count; i++)
            {
                Microsoft.Office.Interop.Word.Shape shape1 = shapes1[i];
                Microsoft.Office.Interop.Word.Range range1 = shape1.TextFrame.TextRange;

                Microsoft.Office.Interop.Word.ContentControls contentControls1 = range1.ContentControls;
                for (int j = 1; j < contentControls1.Count; j++)
                {
                    Microsoft.Office.Interop.Word.ContentControl contentControl1 = contentControls1[j];
                    AddToLog("Tag=" + contentControl1.Tag);
                }
                //AddToLog("Range=" + range1.Text);
            }


            //Microsoft.Office.Interop.Word.ContentControls contentControls1 = doc.ContentControls;
            //Microsoft.Office.Interop.Word.Range range1 = doc.Content;
            //AddToLog("Content=" + range1.Text);
            //for (int i = 1; i <= contentControls1.Count; i++)
            //{
            //    Microsoft.Office.Interop.Word.ContentControl contentControl1 = contentControls1[i];
            //    AddToLog("Tag="+contentControl1.Tag);
            //    AddToLog("Title=" + contentControl1.Title);
            //}

        }
    }
}