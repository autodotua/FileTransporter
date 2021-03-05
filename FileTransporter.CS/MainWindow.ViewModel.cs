using FzLib.Extension;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace FileTransporter
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Log> Logs { get; } = new ObservableCollection<Log>();

        private int maxLogCount = 10000;

        public MainWindowViewModel()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int MaxLogCount
        {
            get => maxLogCount;
            set
            {
                if (value > 100000)
                {
                    value = 100000;
                }
                else if (value < 10)
                {
                    value = 10;
                }
                this.SetValueAndNotify(ref maxLogCount, value, nameof(MaxLogCount));
            }
        }

        private UserControl panel;

        public UserControl Panel
        {
            get => panel;
            set => this.SetValueAndNotify(ref panel, value, nameof(Panel));
        }
    }
}