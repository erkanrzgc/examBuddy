using System;
using System.Windows.Forms;
using System.Drawing;

namespace ExamBuddy
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);

            using var mainForm = new MainForm();
            mainForm.StartPosition = FormStartPosition.Manual;

            var screen = Screen.PrimaryScreen?.WorkingArea ?? Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;
            var desiredWidth = 120 * mainForm.Font.Height / 2; 
            var desiredHeight = 30 * (int)(mainForm.Font.Height * 1.5); 

            desiredWidth = Math.Max(900, desiredWidth);
            desiredHeight = Math.Max(600, desiredHeight);

            mainForm.Size = new Size(desiredWidth, desiredHeight);

            if (screen != Rectangle.Empty)
            {
                var left = Math.Max(screen.Left, screen.Left + (screen.Width - desiredWidth) / 2);
                var top = Math.Max(screen.Top, screen.Top + (screen.Height - desiredHeight) / 2);
                mainForm.Location = new Point(left, top);
            }

            Application.Run(mainForm);
        }
    }
}


