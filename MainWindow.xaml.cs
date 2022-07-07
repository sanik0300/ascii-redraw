using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Windows.Threading;

namespace ASCII_графика
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage loading = new BitmapImage(new Uri("pack://application:,,,/images/tenor_loading.gif"));
        PicturesPair pair;
        string directory;
        int divider, prev;
        static bool prvbool = false;
        BackgroundWorker console_worker = new BackgroundWorker() { WorkerReportsProgress = true };
        public static BackgroundWorker bitmaps_worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
        public MainWindow()
        {
            InitializeComponent();
            //redo this to using triggers
            original.DragEnter += (s, a) => (original.Parent as Border).BorderBrush = System.Windows.Media.Brushes.Green;
            original.DragLeave += (s, a) => (original.Parent as Border).BorderBrush = System.Windows.Media.Brushes.Blue;
            Title += Assembly.GetExecutingAssembly().ImageRuntimeVersion;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher disp = Dispatcher.CurrentDispatcher;
            console_worker.DoWork += (s, ae) =>
            {
                Console.Clear();
                string[] lines = PicturesPair.output.Split('\n');
                for (float i = 0; i < lines.Length; i++)
                {
                    Console.WriteLine(lines[(int)i]);
                    (s as BackgroundWorker).ReportProgress((int)(100 * (i / lines.Length)));
                }
            };
            console_worker.ProgressChanged += (s, ea) => bar.Value = ea.ProgressPercentage;
            console_worker.RunWorkerCompleted += (s, ea) => bar.Value = 0;
            bitmaps_worker.DoWork += (s, ea) => 
                disp.Invoke(() =>
                { 
                    modified.Source = loading;
                    show.IsEnabled = false;
                    ok.Content = "Отмена";                   
                });
            bitmaps_worker.ProgressChanged += (s, ea) => disp.Invoke(() => bar.Value = ea.ProgressPercentage);
            bitmaps_worker.RunWorkerCompleted += (s, ea) => {
                disp.Invoke(() => {
                    if (!PicturesPair.iwscanceld) {
                        pair.LoadToDisplay();
                    }

                    modified.Source = pair.edited_to_display;
                    show.IsEnabled = more.IsEnabled = less.IsEnabled = show.IsEnabled = save.IsEnabled = true;
                    bar.Value = 0;
                    ok.Content = "OK";
                });
            };
        }

        private void Original_Drop(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string path = (e.Data.GetData("FileDrop") as string[])[0];             
                LoadPicture(path);
            }
        }

        private void openfile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.FileOk += Ofd_FileOk;           
            ofd.ShowDialog();
        }
        private void Ofd_FileOk(object sender, CancelEventArgs e) 
        {
            OpenFileDialog current_OFD = sender as OpenFileDialog;
            directory = current_OFD.FileName.Remove((sender as OpenFileDialog).FileName.IndexOf(current_OFD.SafeFileName));
            LoadPicture(current_OFD.FileName);
        }
        
        void LoadPicture(string path)
        {
            try { pair = new PicturesPair(path, bitmaps_worker); }
            catch {
                MessageBox.Show("Выбранный файл битый или не картинка", "нельзя преобразовывать", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            box.IsEnabled = ok.IsEnabled = true;
            display_path.Text = path;       
            divider = prev = pair.min_scale;
            original.Source = pair.orig_to_display;
            box.Text = pair.min_scale.ToString();
            pair.pixelate(divider, invert.IsChecked.Value);
        }

        private void ok_Click(object sender, RoutedEventArgs e)
        {
            if (bitmaps_worker.IsBusy)
            {
                bitmaps_worker.CancelAsync();
            }
            else
            {
                if (prev != divider || invert.IsChecked.Value != prvbool)
                    pair.pixelate(divider, invert.IsChecked.Value);
                prev = Convert.ToInt32(box.Text);
                prvbool = invert.IsChecked.Value;
            }            
        }

        private void CheckForTooBigPicture()
        {
            bool will_fit_in_screen = warning.IsOpen = divider < pair.min_scale;
            box.Background = will_fit_in_screen? System.Windows.Media.Brushes.Yellow : System.Windows.Media.Brushes.White;
        }

        private void more_Click(object sender, RoutedEventArgs e)
        {
            if (divider + 1 <= pair.max_scale)
                divider++;
            else
                more.IsEnabled = false;
            CheckForTooBigPicture();

            box.Text = divider.ToString();
            less.IsEnabled = true;
        }
        private void less_Click(object sender, RoutedEventArgs e)
        {           
            if (divider - 1 > 0)
                divider--;
            else
                less.IsEnabled = false;
            CheckForTooBigPicture();

            box.Text = divider.ToString();
            more.IsEnabled = true;
        }
        private void save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog() { Filter="Текстовые файлы | *.txt", InitialDirectory=directory };
            sfd.FileOk += Sfd_FileOk;
            sfd.ShowDialog();           
        }
        private async void Sfd_FileOk(object sender, CancelEventArgs e)
        {
            using (FileStream fs = new FileStream((sender as SaveFileDialog).FileName, FileMode.CreateNew))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    await sw.WriteAsync(PicturesPair.output);
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e) { Application.Current.Shutdown(); }
        private void invert_Checked(object sender, RoutedEventArgs e) { pair.SetInversion(true); }
        private void invert_Unchecked(object sender, RoutedEventArgs e) { pair.SetInversion(false); }
        
        private void box_TextChanged(object sender, TextChangedEventArgs e)
        {
            box.Text = box.Text.Trim(' ');
            if (string.IsNullOrEmpty(box.Text))
                return;
            try { 
                int perhaps_a_number = Convert.ToInt32(box.Text); 
                if(perhaps_a_number >= 1 && perhaps_a_number <= pair.max_scale) { divider = perhaps_a_number; }
            }
            catch { }
            finally { 
                box.Text = divider.ToString();
                box.CaretIndex = box.Text.Length - 1;
                CheckForTooBigPicture();
            }
        }
      
        private void show_Click(object sender, RoutedEventArgs e) => console_worker.RunWorkerAsync();
    }
}
