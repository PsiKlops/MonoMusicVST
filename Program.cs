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
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //using (var game = new MainMusicGame())
            //    game.Run();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PluginHost.MainForm());

        }
    }
//#endif
}
