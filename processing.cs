using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;


namespace ASCII_графика
{
    /// <summary>
    /// data structure to compress and bleach pictures
    /// </summary>
    public class PicturesPair
    {
        private static string[] symbols = new string[] { "#", "Y", "/", "=", ":", " ", " " };
        public static string output = string.Empty;

        public BitmapImage orig_to_display { get; private set; }     
        public BitmapImage edited_to_display { get; private set; }
        private Bitmap original, edited;
        public int min_scale { get; }
        public int max_scale { get; }
        private bool inv;
        public void SetInversion(bool val) { this.inv = val;}
        
        private BackgroundWorker worker;
        public PicturesPair(string filepath, BackgroundWorker w)
        {
            original = new Bitmap(filepath);
            orig_to_display = new BitmapImage(new Uri(filepath));
            min_scale = 1;
            worker = w;
            worker.DoWork += (s, ea) =>
            {
                lock (new object())
                {
                    ea.Result = ea.Argument;
                    grayscale(ea.Result as Bitmap);                 
                }
            };
            worker.RunWorkerCompleted += (s, ea) => this.edited = ea.Result as Bitmap;

            if(original.Width*16 > SystemParameters.VirtualScreenWidth || original.Height*16 > SystemParameters.VirtualScreenHeight)
            {
                double min_x = Math.Ceiling((double)original.Width * 16 / SystemParameters.VirtualScreenWidth),
                    min_y = Math.Ceiling((double)original.Height * 16 / SystemParameters.VirtualScreenHeight);
                min_scale = (min_x > min_y)? (int)min_x : (int)min_y;
            }
            int max_x = original.Width / 4, max_y = original.Height / 4;
            max_scale = (max_x < max_y) ? max_x : max_y;
        }
        public void LoadToDisplay()
        {
            BitmapImage edited_to_display = new BitmapImage();
            this.edited = new Bitmap(edited, edited.Width / 2, edited.Height);
            using (MemoryStream mem = new MemoryStream())
            {
                edited.Save(mem, ImageFormat.Bmp);
                mem.Position = 0;
                edited_to_display.BeginInit();
                edited_to_display.StreamSource = mem;
                edited_to_display.CacheOption = BitmapCacheOption.OnLoad;
                edited_to_display.EndInit();
            }
            this.edited_to_display = edited_to_display;
        }
        public void pixelate(int scale, bool invert)
        {            
            this.edited = new Bitmap(this.original, (this.original.Width / scale)*2, this.original.Height / scale);
            this.inv = invert;
            worker.RunWorkerAsync(this.edited);
        }
        static public bool iwscanceld;
        private void grayscale(Bitmap bmp)
        {
            string result = string.Empty;
            for (int i = 0; i < bmp.Height; i++)
            {
                for (int k = 0; k < bmp.Width; k++)
                {
                    iwscanceld = MainWindow.bitmaps_worker.CancellationPending;
                    if (MainWindow.bitmaps_worker.CancellationPending)
                    {                        
                        return;
                    }
                    UInt32 pixel = (UInt32)bmp.GetPixel(k, i).ToArgb();
                    float r = ((pixel & 0x00FF0000) >> 16);
                    float g = ((pixel & 0x0000FF00) >> 8);
                    float b = (pixel & 0x000000FF);
                    r = g = b = (r + g + b) / 3;

                    int index = (int)Math.Round(r / (255 / (symbols.Length - 1)));
                    if (this.inv) { index = symbols.Length - 1 - index; }
                    result = result + symbols[index];

                    UInt32 newPixel = 0xFF000000 | ((UInt32)r << 16) | ((UInt32)g << 8) | (UInt32)b;
                    bmp.SetPixel(k, i, Color.FromArgb((int)newPixel));
                }
                result = result + "\n";
                worker.ReportProgress((int)(100 * ((i+1) / (float)bmp.Height)));
            }
            bmp.Tag = "ч/б";
            output = result;
        }
    }
}
