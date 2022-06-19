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
        string msg = "client";
        string login = "";
        bool enable = true;
        string conText = "Connect";
        bool connect = true;
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
            Enable = false;
            ConText = "Connected";
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
            bool flag = false;
            while (true)
            {
                byte[] buf = new byte[size];
                int bytesReceived = clientSocket.Receive(buf);
                string[] infoPackage = Encoding.UTF8.GetString(buf).Trim('\0').Split('~');
                if (infoPackage[0] == "F")
                {
                    flag = true;
                    FileStream file = File.Create(infoPackage[1]);
                    GetMsg += $"[INFO] Receiving file {infoPackage[1]} from {infoPackage[2]}...\n";
                    while (flag)
                    {
                        bytesReceived = clientSocket.Receive(buf);
                        if (Encoding.UTF8.GetString(buf).Trim('\0') == "file_sended")
                        {
                            GetMsg += "Successful!\n";
                            file.Close();
                            flag = false;
                        }
                        else
                            file.Write(buf, 0, bytesReceived);
                    }
                }
                else
                    GetMsg += Encoding.UTF8.GetString(buf).Trim('\0');               
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
            GetMsg += $"{login}(You) >> " + GetText + "\n";
        }

        public void SendFile()
        {
            //Button SendFile

            //window.InitializeComponent();
            //var window = new FilesWindow();
            //window.Show();
            string filePath = GetText;
            string fileName = "";
            byte[] buf = new byte[size];
            try
            {
                fileName = filePath.Substring(filePath.LastIndexOf('\\') + 1);
                foreach (Socket sock in clients)
                {
                    buf = Encoding.UTF8.GetBytes($"F~{fileName}~{login}");
                    sock.Send(buf);
                    Thread.Sleep(10);
                    sock.SendFile(fileName);
                    Thread.Sleep(1000);
                    buf = Encoding.UTF8.GetBytes($"file_sended");
                    sock.Send(buf);
                }
                GetText = "";
            }
            catch (Exception ex)
            {
                GetMsg += "[INFO] File is not exist! Check file path.\n";
            }
            /*foreach (var item in clients)
            {
                item.Send(Encoding.UTF8.GetBytes($"f{(new FileInfo("1.png")).Length / fileSize}"));
                item.SendFileAsync("1.png");
            }*/
        }
    }
}
