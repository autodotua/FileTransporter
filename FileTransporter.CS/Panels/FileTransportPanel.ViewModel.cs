using FileTransporter.Dto;
using FileTransporter.FileSimpleSocket;
using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using FzLib.Extension;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FileTransporter.Panels
{
    public class FileTransportPanelViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<TransportFile> files = new ObservableCollection<TransportFile>();
        private TransportFile selectedFile;
        private FilePanelType type;
        private bool waiting;
        private bool working;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TransportFile> Files
        {
            get => files;
            set => this.SetValueAndNotify(ref files, value, nameof(Files));
        }

        public TransportFile SelectedFile
        {
            get => selectedFile;
            set => this.SetValueAndNotify(ref selectedFile, value, nameof(SelectedFile));
        }

        public bool Stopping
        {
            get => waiting;
            set => this.SetValueAndNotify(ref waiting, value, nameof(Stopping));
        }

        public FilePanelType Type
        {
            get => type;
            set => this.SetValueAndNotify(ref type, value, nameof(Type));
        }

        public bool Working
        {
            get => working;
            set => this.SetValueAndNotify(ref working, value, nameof(Working));
        }
    }
}