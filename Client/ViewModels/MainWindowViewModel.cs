using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using ReactiveUI;
using System.Collections.ObjectModel;
using Client.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using System.Threading.Tasks;

namespace Client.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        IPEndPoint BroadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, 12122);
        IPEndPoint addr = new IPEndPoint(IPAddress.Any, 12122);
        IPEndPoint myIPEndPoint;
        int port;
        UdpClient s = new UdpClient();
        IPEndPoint ip = null;
        TcpListener Listener;
        List<Thread> readingThreads = new List<Thread>();
        List<Socket> clients = new List<Socket>();

        const int size = 512;
        const int fileSize = 4096;
        string str = "";
        string msg = "";
        string login = "";
        bool enable = true;
        string conText = "Connect";

        static ObservableCollection<IPAddress> networks = new ObservableCollection<IPAddress>();
        static public ObservableCollection<IPAddress> Networks
        {
            get { return networks; }
            set { networks = value; }
        }

        public string GetText //Texbox send message
        {
            get => str;
            set => this.RaiseAndSetIfChanged(ref str, value);
        }
        public string GetMsg //Textbox messages
        {
            get => msg;
            set => this.RaiseAndSetIfChanged(ref msg, value);
        }

        public string Login //Text Login
        {
            get => login;
            set => this.RaiseAndSetIfChanged(ref login, value);
        }

        public bool Enable //Button Connection
        {
            get => enable;
            set => this.RaiseAndSetIfChanged(ref enable, value);
        }

        public string ConText //Button Connection
        {
            get => conText;
            set => this.RaiseAndSetIfChanged(ref conText, value);
        }

        IPAddress selectNetwork = Networks[0];
        public IPAddress SelectNetwork
        {
            get => selectNetwork;
            set
            {
                selectNetwork = Networks[Networks.IndexOf(value)];
            }
        }


        public void Auth()
        {
            //Button Connect
            //Подготовка сокета UPD для отправки сообщения в шировещательный канал;
            s.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            s.Client.Bind(addr);
            //Подбираем порт для TCPListener
            bool findPort = true;
            for (port = 8080; (findPort) && (port < 8090);)
            {
                try
                {
                    Listener = new TcpListener(port);
                    Listener.Start();
                    findPort = false;
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    GetMsg = "Поиск свободного порта";
                    port++;
                }
            }
            GetMsg = $"My port: {port}\n";
            myIPEndPoint = new IPEndPoint(SelectNetwork, port);
            //Запускаем принятие подключений после нашего сообщение в широковещательный по UDP
            Thread ConnectOldSoket = new Thread(() =>
            {
                while (true)
                {
                    Socket newSock = Listener.AcceptSocket();
                    clients.Add(newSock);
                    readingThreads.Add(new Thread(() =>
                    {
                        ReadMessage(clients[clients.Count - 1]);
                    }));
                    readingThreads[readingThreads.Count - 1].Start();
                }
            });
            ConnectOldSoket.Start();
            //Отправляем наш TCPListener в широковещательный по UDP
            byte[] buf = Encoding.UTF8.GetBytes($"{myIPEndPoint.ToString()}");
            s.Send(buf, buf.Length, BroadcastEndPoint);
            //Подключение сокетов, созданых после нашего
            Thread findClientThread = new Thread(FindNewClient);
            findClientThread.Start();
        }

        private void FindNewClient()
        {
            while (true)
            {
                byte[] buf = new byte[100];
                buf = s.Receive(ref ip);
                string ipReceive = Encoding.UTF8.GetString(buf);
                if (ipReceive != myIPEndPoint.ToString())
                {
                    clients.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
                    clients[clients.Count - 1].Connect(IPEndPoint.Parse(ipReceive));
                    readingThreads.Add(new Thread(() =>
                    {
                        ReadMessage(clients[clients.Count - 1]);
                    }));
                    readingThreads[readingThreads.Count - 1].Start();
                }
            }
        }
        public void ReadMessage(Socket clientSocket)
        {
            while (true)
            {
                byte[] serviceBuf = new byte[4];
                clientSocket.Receive(serviceBuf);
                string serviceMsg = Encoding.UTF8.GetString(serviceBuf).Trim('\0');
                if (serviceMsg == "m")
                {
                    byte[] buf = new byte[size];
                    clientSocket.Receive(buf);
                    GetMsg += "•" + Encoding.UTF8.GetString(buf).Replace("\0", "") + "\n";
                }
                else if (serviceMsg.Contains("f"))
                {
                    FileStream file = new FileInfo("C:\\User files\\1.png").Create();
                    for (int i = 0; i <= int.Parse(serviceMsg.Substring(1)); i++)
                    {
                        byte[] buf = new byte[fileSize];
                        clientSocket.Receive(buf);
                        file.Write(buf, 0, buf.Length);
                    }
                    file.Close();
                }
            }
        }

        public void SendMsg()
        {
            //Button Send
            byte[] buf = new byte[size];
            buf = Encoding.UTF8.GetBytes($"{Login} >> {GetText}");
            foreach (var item in clients)
            {
                item.Send(Encoding.UTF8.GetBytes("m"));
                item.Send(buf);
            }
            GetMsg += "•I'm >> " + GetText + "\n";
        }

        public async void SendFile()
        {
            //Button SendFile
            var window = new FilesWindow();
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                await window.ShowDialog(desktop.MainWindow);
            }
            GetMsg += $"Filepath: {(window.DataContext as FilesWindowViewModel).FilePath}";

            /*foreach (var item in clients)
            {
                item.Send(Encoding.UTF8.GetBytes($"f{(new FileInfo("1.png")).Length / fileSize}"));
                item.SendFileAsync("1.png");
            }*/
        }
    }
}
