using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;

namespace Battleships
{

    //по образу и подобию https://metanit.com/sharp/net/3.2.php
    public class Network : INotifyPropertyChanged
    {
        private bool _isServerActive;
        private Thread thread;
        private Dispatcher dispatcher;
        private int port;
        private IPAddress ip;
        private IPEndPoint _ipEndPoint;
        private Socket listenSocket;
        private Socket connection;

        public Network()
        {
            port = 8005;
            ip = IPAddress.Parse("127.0.0.1");
            this.dispatcher = Dispatcher.CurrentDispatcher;
        }
        string _status;
        public String Status
        {
            get { return _status; }
            set
            {
                if (value != _status)
                {
                    _status = value;
                    OnPropertyChanged("Status");
                }
            }

        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool VerifyIP(string ip)
        {
            return false;
        }

        public string IP
        {
            get
            {
                if (ip == null || ip.ToString()=="127.0.0.1")
                    return "";
                return ip.ToString();
            }

        }

        public bool SetIP(string ip_)
        {
            return IPAddress.TryParse(ip_, out ip);
        }

        public string Listen()
        {
            StringBuilder msg = new StringBuilder();

            if (_ipEndPoint == null)
                _ipEndPoint = new IPEndPoint(IPAddress.Any, port); //У компьютера может быть несколько сетевых плат с несколькими IP-адресами. 
                                                           //Сокет использует IPAddress.Any, чтобы ожидать действия на любом из этих сетевых интерфейсов.

            if (listenSocket == null)
            {
                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // связываем сокет с локальной точкой, по которой будем принимать данные
                listenSocket.Bind(_ipEndPoint);
            }

            try
            {

                if (connection == null)
                {
                    // начинаем прослушивание
                    listenSocket.Listen(10);
                    connection = listenSocket.Accept();
                }

                // получаем сообщение
                int bytes = 0;               // количество полученных байтов
                byte[] data = new byte[256]; // буфер для получаемых данных
                do
                {
                    bytes = connection.Receive(data);
                    msg.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (connection.Available > 0);
            }
            catch (Exception ex)
            {
                return "#error " + ex.Message;
            }

            return msg.ToString();
        }

        public bool Send(string msg)
        {
            if (_ipEndPoint == null)
                _ipEndPoint = new IPEndPoint(ip, port);
            try
            {
                if (listenSocket == null)
                {
                    listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    listenSocket.Connect(_ipEndPoint);
                }
                // связываем сокет с локальной точкой, по которой будем принимать данные

                byte[] data = new byte[256]; // буфер для получаемых данных
                data = Encoding.Unicode.GetBytes(msg);
                listenSocket.Send(data);

            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public void KillSocket()
        {
            if (connection != null)
                connection.Shutdown(SocketShutdown.Both);
            if (listenSocket != null)
                listenSocket.Close();
        }

        public void StartServer()
        {
            if (this.IsServerActive) return;

            if (_ipEndPoint == null)
                _ipEndPoint = new IPEndPoint(IPAddress.Any, port); //У компьютера может быть несколько сетевых плат с несколькими IP-адресами. 
                                                                   //Сокет использует IPAddress.Any, чтобы ожидать действия на любом из этих сетевых интерфейсов.

            if (listenSocket == null)
            {
                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // связываем сокет с локальной точкой, по которой будем принимать данные
                listenSocket.Bind(_ipEndPoint);
                listenSocket.Listen(5);
            }

            thread = new Thread(new ThreadStart(WaitForConnections));
            thread.Start();

            this.IsServerActive = true;
        }

        private void WaitForConnections()
        {
            while (true)
            {
                if (listenSocket == null) return;
                Client client = new Client();
                client.PlayerName = "NewUser"; // Временное имя пользователя
                try
                {
                    client.Socket = listenSocket.Accept();
                    client.Thread = new Thread(() => ProcessMessages(client));
                    this.dispatcher.Invoke(new Action(() =>
                    {
                        Status="#Входящее соединение";
                    }), null);
                    client.Thread.Start();
                }
                catch (Exception)
                {
                    //MessageBox.Show(ex.Message, "Error");
                }
            }
        }

        void ProcessMessages(Client c)
        {
            while (true)
            {
                try
                {
                    if (!c.IsSocketConnected())
                    {
                        this.dispatcher.Invoke(new Action(() =>
                        {
                            Status = "#Обрыв соединения";
                        }), null);

                        return;
                    }

                    byte[] inf = new byte[1024];
                    int x = c.Socket.Receive(inf);
                    if (x > 0)
                    {
                        string strMessage = Encoding.Unicode.GetString(inf);
                        // check and execute commands
                        if (strMessage.Substring(0, 8) == "/setname")
                        {
                            string newUsername = strMessage.Replace("/setname ", "").Trim('\0');

                            c.Username = newUsername;
                        }
                        else if (strMessage.Substring(0, 6) == "/msgto")
                        {
                            string data = strMessage.Replace("/msgto ", "").Trim('\0');
                            string targetUsername = data.Substring(0, data.IndexOf(':'));
                            string message = data.Substring(data.IndexOf(':') + 1);

                            this._dispatcher.Invoke(new Action(() =>
                            {
                                SendMessage(c, targetUsername, message);
                            }), null);
                        }

                    }
                }
                catch (Exception)
                {
                    this._dispatcher.Invoke(new Action(() =>
                    {
                        lstClients.Remove(c);
                        c.Dispose();
                    }), null);
                    return;
                }
            }
        }

        public bool IsServerActive
        {
            get
            {
                return _isServerActive;
            }
            private set
            {
                this._isServerActive = value;

                //this.NotifyPropertyChanged("IsServerActive");
                //this.NotifyPropertyChanged("IsServerStopped");
            }
        }
    }
}
