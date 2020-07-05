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
        private Thread thread;
        private Dispatcher dispatcher;
        public List<Client> lstClients { get; set; }

        private CancellationTokenSource waitConnectionToken,waitMessagesToken;
        private int port;
        private IPAddress ip;
        private IPEndPoint ipEndPoint;
        private Socket listenSocket;
        private Socket connection;
        private Client client;
        private string myName;

        public Network(string _myName = "")
        {
            port = 8005;
            ip = IPAddress.Parse("127.0.0.1");
            this.dispatcher = Dispatcher.CurrentDispatcher;
            myName = _myName;
            client = new Client();
            this.lstClients = new List<Client>();
        }

        string status;

       
        public String State
        {
            get { return status; }
            set
            {
                status = value;
                OnPropertyChanged("State");
            }

        }

        public string MyName
        {
            get { return myName; }
            set
            {
                if (value != myName)
                {
                    myName = value;
                    OnPropertyChanged("MyName");
                }
            }
        }

        string playerName;
        public string PlayerName
        {
            get { return playerName; }
            set
            {
                this.playerName = value;
                this.lstClients[0].PlayerName = value;
                OnPropertyChanged("PlayerName");
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
                if (ip == null || ip.ToString() == "127.0.0.1")
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

            if (ipEndPoint == null)
                ipEndPoint = new IPEndPoint(IPAddress.Any, port); //У компьютера может быть несколько сетевых плат с несколькими IP-адресами. 
                                                                  //Сокет использует IPAddress.Any, чтобы ожидать действия на любом из этих сетевых интерфейсов.

            if (listenSocket == null)
            {
                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // связываем сокет с локальной точкой, по которой будем принимать данные
                listenSocket.Bind(ipEndPoint);
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

        public void StartServer()
        {
            if (IsServerActive) return;

            try
            {
                //У компьютера может быть несколько сетевых плат с несколькими IP-адресами. 
                //Сокет использует IPAddress.Any, чтобы ожидать действия на любом из этих сетевых интерфейсов.
                ipEndPoint = new IPEndPoint(IPAddress.Any, port);

                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // связываем сокет с локальной точкой, по которой будем принимать данные
                listenSocket.Bind(ipEndPoint);
                listenSocket.Listen(1);

                waitConnectionToken = new CancellationTokenSource();
                waitMessagesToken = new CancellationTokenSource();
                thread = new Thread(() => WaitForConnections(waitConnectionToken.Token, waitMessagesToken.Token));
                thread.Start();
                this.IsServerActive = true;
                State = "Server is active. Wait connection";

            }
            catch (Exception ex)
            {
                //что то пошло не так
                //подчищаем хвосты
                StopServer();
                //сообщаем об ошибке
                State = "/Error -Start server fail! " + ex.Message + "Source: " + ex.Source;
            }
        }

        public void StartConnection()
        {
            if (this.IsClientConnected) return;

            client = new Client();
            try
            {
                ipEndPoint = new IPEndPoint(ip, port);
                client.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Socket.Connect(ipEndPoint);
                client.PlayerName = "NewUser";

                this.dispatcher.Invoke(new Action(() =>
                {
                    lstClients.Add(client);
                }), null);

                SendMessage("/go_battle " + MyName);
                IsClientConnected = true;
                State = "Сonnected. Awaiting confirmation";

                waitMessagesToken = new CancellationTokenSource();
                client.Thread = new Thread(() => GetMessages(client, waitMessagesToken.Token));
                client.Thread.Start();
            }
            catch (Exception ex)
            {
                State = "/Error -Create connection fail! " + ex.Message;
            }
        }

       private void WaitForConnections(CancellationToken _waitConnectionToken, CancellationToken _waitMessagesToken)
        {
            while (true)
            {
                if (listenSocket == null) return;
                client = new Client();
                client.PlayerName = "NewUser"; // Временное имя пользователя

                try
                {
                    
                    client.Socket = listenSocket.Accept();
                    client.Thread = new Thread(() => GetMessages(client,_waitMessagesToken));
                    client.Thread.Start();

                    this.dispatcher.Invoke(new Action(() =>
                    {
                        lstClients.Add(client);
                    }), null);

                }
                catch (Exception ex)
                {
                    State = "/Error -Accept connection fail! " + ex.Message;
                }

                if (_waitConnectionToken.IsCancellationRequested)
                    return;

            }
        }

       private void GetMessages(Client client, CancellationToken token)
        {
            //ожидаем в бесконечном цикле сообщения
            while (true)
            {
                try
                {
                    byte[] inf = new byte[1024];
                    int x = lstClients[0].Socket.Receive(inf);
                    if (x > 0)
                    {
                        string strMessage = Encoding.Unicode.GetString(inf);
                        //выполняем полученную команду
                        if (strMessage.Substring(0, 10) == "/go_battle")
                        {
                            string newUsername = strMessage.Replace("/go_battle ", "").Trim('\0');
                            SendMessage("/my_name " + myName);
                            this.dispatcher.Invoke(new Action(() =>
                            {
                                PlayerName = newUsername;
                                State = "/go_battle";
                            }), null);
                            continue;

                        }
                        if (strMessage.Substring(0, 8) == "/my_name")
                        {
                            string newUsername = strMessage.Replace("/my_name ", "").Trim('\0');
                            this.dispatcher.Invoke(new Action(() =>
                            {
                                PlayerName = newUsername;
                            }), null);
                            continue;
                        }
                        if (strMessage.Substring(0, 4) == "/Yes")
                        {
                            this.dispatcher.Invoke(new Action(() =>
                            {
                                State = "/Yes";
                            }), null);
                            continue;
                        }
                        if (strMessage.Substring(0, 3) == "/No")
                        {
                            this.dispatcher.Invoke(new Action(() =>
                            {
                                State = "/No";
                            }), null);
                            StopConnection();
                            break;
                        }
                        if (strMessage.Substring(0, 7) == "/Battle")
                        {
                            this.dispatcher.Invoke(new Action(() =>
                            {
                                State = "/Battle";
                            }), null);
                            continue;
                        }
                        if (strMessage.Substring(0, 4) == "/hit")
                        {
                            string xy = strMessage.Replace("/hit", "").Trim('\0');
                            this.dispatcher.Invoke(new Action(() =>
                            {
                                State = "/hit" + xy;
                            }), null);
                            continue;
                        }
                        if (strMessage.Substring(0, 14) == "/AttackResult ")
                        {
                            string AttackResult = strMessage.Trim('\0');
                            this.dispatcher.Invoke(new Action(() =>
                            {
                                State =  AttackResult;
                            }), null);
                            continue;
                        }

                    }
                }
                catch (Exception)
                {
                    this.dispatcher.Invoke(new Action(() =>
                    {
                        lstClients.Remove(client);
                        client.Dispose();
                    }), null);
                    return;
                }
                if (token.IsCancellationRequested || !IsSocketConnected(lstClients[0].Socket))
                {
                    this.dispatcher.Invoke(new Action(() =>
                    {
                        State = "/Disconnect";
                    }), null);
                    return;
                }
            }
        }

        public void SendMessage(string msg)
        {
            this.SendMessage(client, msg);
        }

        private void SendMessage(Client from, string strMessage)
        {
            try
            {
                lstClients[0].Socket.Send(Encoding.Unicode.GetBytes(strMessage));
            }
            catch (Exception ex)
            {
                State = "/Error -Send message fail! " + ex.Message;
            }
        }

        public bool IsServerActive
        {
            get;
            private set;
        }

        public bool IsClientConnected
        {
            get;
            private set;
        }

        public static bool IsSocketConnected(Socket _socket)
        {
            if (!_socket.Connected)
                return false;

            if (_socket.Available == 0 && _socket.Poll(1000, SelectMode.SelectRead))
                return false;

            return true;
        }

        public void StopConnection()
        {
            try
            {
                waitMessagesToken.Cancel();
                lstClients[0].Dispose();
                lstClients.RemoveAt(0);
                IsClientConnected = false;
            }
            catch (Exception) { }
        }

        public void StopServer()
        {
            if (this.thread != null)
            {
                waitConnectionToken.Cancel();
            }
            if(listenSocket!=null && listenSocket.Connected)
                listenSocket.Shutdown(SocketShutdown.Both);
            listenSocket.Close();

            this.IsServerActive = false;
        }

        ~Network()
        {
            StopConnection();
            if (IsServerActive)
                StopServer();

        }
    }
}
