using FzLib.Basic;
using FzLib.Extension;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace FileTransporter.Model
{
    public class TransporterFile : INotifyPropertyChanged
    {
        public TransporterFile(string file)
        {
            File = new FileInfo(file);
        }

        public TransporterFile()
        { }

        public event PropertyChangedEventHandler PropertyChanged;

        private FileInfo file;
        private Guid iD;

        public Guid ID
        {
            get => iD;
            set => this.SetValueAndNotify(ref iD, value, nameof(ID));
        }

        public FileInfo File
        {
            get => file;
            private set
            {
                Name = value.Name;
                Path = value.FullName;
                Length = value.Length;
                time = value.LastWriteTime;
                this.SetValueAndNotify(ref file, value, nameof(File));
            }
        }

        private long transportedLength;

        public long TransportedLength
        {
            get => transportedLength;
            set => this.SetValueAndNotify(ref transportedLength, value, nameof(TransportedLength));
        }

        private long length;

        public long Length
        {
            get => length;
            set => this.SetValueAndNotify(ref length, value, nameof(Length));
        }

        private DateTime time;

        public DateTime Time
        {
            get => time;
            set => this.SetValueAndNotify(ref time, value, nameof(Time));
        }

        private string name;

        public string Name
        {
            get => name;
            set => this.SetValueAndNotify(ref name, value, nameof(Name));
        }

        private string path;

        public string Path
        {
            get => path;
            set => this.SetValueAndNotify(ref path, value, nameof(Path));
        }

        private int percent;

        public int Percent
        {
            get => percent;
            set => this.SetValueAndNotify(ref percent, value, nameof(Percent), nameof(Finished));
        }

        public void UpdateProgress(long sendedByteCount)
        {
            Percent = (int)(100.0 * sendedByteCount / Length);
            TransportedLength = sendedByteCount;
        }

        public bool Finished => Percent == 100;
    }
}