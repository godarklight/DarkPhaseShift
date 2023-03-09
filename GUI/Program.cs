using DarkPhaseShift.Common;
using System;
using Gtk;

namespace DarkPhaseShift.GUI
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AudioDriver audio = new AudioDriver(true, null, false);
            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => { audio.Stop(); };

            Application.Init();

            var app = new Application("org.DarkPhaseShift.DarkPhaseShift", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            var win = new MainWindow(audio);
            app.AddWindow(win);
            win.DeleteEvent += (object sender, DeleteEventArgs e) => { audio.Stop(); };

            win.Show();
            Application.Run();
        }
    }
}
