using System;

namespace ASCII_графика
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
