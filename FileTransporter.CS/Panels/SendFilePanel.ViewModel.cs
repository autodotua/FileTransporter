using FileTransporter.FileSimpleSocket;
using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using FzLib.Extension;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FileTransporter.Panels
{
    public class SendFilePanelViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<TransporterFile> Files { get; } = new ObservableCollection<TransporterFile>();

        private bool sending;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Sending
        {
            get => sending;
            set => this.SetValueAndNotify(ref sending, value, nameof(Sending));
        }

        private bool waiting;

        public bool Stopping
        {
            get => waiting;
            set => this.SetValueAndNotify(ref waiting, value, nameof(Stopping));
        }
    }
}