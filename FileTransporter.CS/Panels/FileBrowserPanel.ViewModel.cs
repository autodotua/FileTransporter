using FileTransporter.FileSimpleSocket;
using FileTransporter.Model;
using FileTransporter.SimpleSocket;
using FzLib.Extension;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FileTransporter.Panels
{
    public class FileBrowserPanelViewModel : INotifyPropertyChanged
    {
        private FileListInfo selectedFile;

        public event PropertyChangedEventHandler PropertyChanged;

        private string path;

        public string Path
        {
            get => path;
            set => this.SetValueAndNotify(ref path, value, nameof(Path), nameof(CanGotoParentDir));
        }

        public bool CanGotoParentDir => Path == null ? false : Path.Where(p => p == '\\' || p == '/').Count() > 0;

        public ObservableCollection<FileListInfo> Files { get; } = new ObservableCollection<FileListInfo>();

        public FileListInfo SelectedFile
        {
            get => selectedFile;
            set => this.SetValueAndNotify(ref selectedFile, value, nameof(SelectedFile));
        }

        private ClientSocketHelper socket;

        public ClientSocketHelper Socket
        {
            get => socket;
            set
            {
                socket = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Socket)));
            }
        }
    }
}