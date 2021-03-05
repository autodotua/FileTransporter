using FzLib.Basic;
using FzLib.Extension;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace FileTransporter.Panels
{
    public class ClientPanelViewModel
    {
        public ObservableCollection<TransporterFile> Files { get; } = new ObservableCollection<TransporterFile>();
    }

    public class TransporterFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private FileInfo file;

        public FileInfo File
        {
            get => file;
            private set
            {
                Length = FzLib.Basic.Number.ByteToFitString(value.Length);
                this.SetValueAndNotify(ref file, value, nameof(File));
            }
        }

        private string length;

        public string Length
        {
            get => length;
            set => this.SetValueAndNotify(ref length, value, nameof(Length));
        }

        private int percent;

        public TransporterFile(string file)
        {
            File = new FileInfo(file);
        }

        public TransporterFile()
        {
        }

        public int Percent
        {
            get => percent;
            set => this.SetValueAndNotify(ref percent, value, nameof(Percent));
        }

        public void UpdateProgress(long sendedByteCount)
        {
            Percent = (int)(100.0 * sendedByteCount / file.Length);
            Length = Number.ByteToFitString(sendedByteCount) + " / " + Number.ByteToFitString(file.Length);
        }
    }
}