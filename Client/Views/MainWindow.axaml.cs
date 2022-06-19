using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.Net;

namespace Client.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            IPAddress[] IPs = Dns.GetHostByName(Dns.GetHostName()).AddressList;
            foreach (var item in IPs)
            {
                if (item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    Client.ViewModels.MainWindowViewModel.Networks.Add(item);
            }
        }
        public void WindowClosed(object sender, EventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}
