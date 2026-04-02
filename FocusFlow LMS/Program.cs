using FocusFlow_LMS.Data;
using FocusFlow_LMS.Forms;

namespace FocusFlow_LMS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // High DPI & visual styles
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Dark mode title bar on Windows 11
            try
            {
                if (Environment.OSVersion.Version.Build >= 22000)
                {
                    // Windows 11: enable dark title bar via registry hint (applied per form)
                }
            }
            catch { }

            // Initialize SQLite database
            DatabaseManager.Initialize();

            Application.Run(new MainForm());
        }
    }
}
