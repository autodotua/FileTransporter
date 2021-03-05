﻿using FileTransporter.FileSimpleSocket;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace FileTransporter.Panels
{
    public partial class ClientPanel : UserControl
    {
        public ClientPanelViewModel ViewModel { get; set; }

        public ClientPanel(ClientSocketHelper socket)
        {
            ViewModel = new ClientPanelViewModel(socket);
            DataContext = ViewModel;
            InitializeComponent();
            //sfp.Socket = socket;
        }
    }
}