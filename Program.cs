using System;
using System.Windows.Forms;

namespace MonoMusicMaker
{
//#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        static bool mMainForm = false;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            if (mMainForm)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new PluginHost.MainForm(null));
            }
            else
            {
                using (var game = new MainMusicGame())
                    game.Run();
            }
        }
    }
//#endif
}
