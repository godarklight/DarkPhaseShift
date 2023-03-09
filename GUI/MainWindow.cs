using DarkPhaseShift.Common;
using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace DarkPhaseShift.GUI
{
    class MainWindow : Window
    {
        [UI] Adjustment adjustRange = null;
        [UI] Scrollbar scrollPhase = null;
        [UI] Label lblStatus = null;
        AudioDriver audio;


        public MainWindow(AudioDriver audio) : this(new Builder("MainWindow.glade"))
        {
            this.audio = audio;
        }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            lblStatus.Text = "Phase shift: 90";
            scrollPhase.ValueChanged += phaseChanged;
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private void phaseChanged(object sender, EventArgs a)
        {
            lblStatus.Text = $"Phase shift {scrollPhase.Value}";
        }
    }
}
