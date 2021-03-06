using FileTransporter.FileSimpleSocket;
using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using FzLib.Extension;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FileTransporter.Panels
{
    public class FilePanelViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<TransportFile> Files { get; } = new ObservableCollection<TransportFile>();
        private TransportFile selectedFile;

        public TransportFile SelectedFile
        {
            get => selectedFile;
            set => this.SetValueAndNotify(ref selectedFile, value, nameof(SelectedFile));
        }

        private FilePanelType type;

        public FilePanelType Type
        {
            get => type;
            set => this.SetValueAndNotify(ref type, value, nameof(Type));
        }

        private bool working;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Working
        {
            get => working;
            set => this.SetValueAndNotify(ref working, value, nameof(Working));
        }

        private bool waiting;

        public bool Stopping
        {
            get => waiting;
            set => this.SetValueAndNotify(ref waiting, value, nameof(Stopping));
        }
    }
}