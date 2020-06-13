﻿using System;
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
        }

        string status;
        public String State
        {
            get { return status; }
            set
            {
                if (value != status)
                {
                    status = value;
                    OnPropertyChanged("State");
                }
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

        public string PlayerName
        {
            get { return client.PlayerName; }
            set
            {
                this.dispatcher.Invoke(new Action(() =>
                {
                    client.PlayerName = value;
                    OnPropertyChanged("PlayerName");
                }), null);

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

        public void KillSocket(Socket socket)
        {

            if (socket == null) return;
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {

            }
            finally
            {
                socket.Close();
            }
        }

        public async void StartServer()
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


                this.IsServerActive = true;
                State = "Server is active. Wait connection";

                //CancellationTokenSource - специальный механизм который позволяет отправить в паралельно выполняющийся поток сигнал о заверешении этого потока
                waitConnectionToken = new CancellationTokenSource();
                await Task.Run(() => this.WaitForConnections(waitConnectionToken.Token)); //WaitForConnections (ожидание подключений) запускаем в паралельном потоке чтобы не зависал интерфейс
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

        public void StopServer()
        {
            if (this.thread != null)
            {
                waitConnectionToken.Cancel();
            }

            KillSocket(listenSocket);

            if (ipEndPoint != null)
                ipEndPoint = null;

            this.IsServerActive = false;
        }

        private void WaitForConnections(CancellationToken token)
        {
            while (true)
            {
                if (listenSocket == null) return;
                client = new Client();
                //PlayerName = "NewUser"; // Временное имя пользователя
                try
                {
                    client.Socket = listenSocket.Accept();

                    //как только соединение установлено получаем первое собщение от клиента в котором должно содержать имя игрока бросившего нам вызов
                    GetMessage(client);
                    
                }
                catch (Exception ex)
                {
                    State = "/Error -Accept connection fail! " + ex.Message;
                }
                if (token.IsCancellationRequested)
                    return;
            }
        }

        public async void WaitMessage()
        {
            waitMessagesToken = new CancellationTokenSource();
            await Task.Run(() => this.GetMessages(this.client, waitMessagesToken.Token));
        }

        private void GetMessages(Client client, CancellationToken token)
        {
            //ожидаем в бесконечном цикле сообщения
            while (true)
            {
                GetMessage(client);
                if (token.IsCancellationRequested)
                    return;
            }
        }

        private void GetMessage(Client client)
        {
            try
            {
                byte[] inf = new byte[1024];
                int x = client.Socket.Receive(inf);
                if (x > 0)
                {
                    string strMessage = Encoding.Unicode.GetString(inf);
                    
                    //выполняем полученную команду
                    if (strMessage.Substring(0, 9) == "/go_battle")
                    {
                        string newUsername = strMessage.Replace("/go_battle ", "").Trim('\0');
                        SendMessage("/my_name " + myName);

                        this.dispatcher.Invoke(new Action(() =>
                        {
                            PlayerName = newUsername;
                            State = "/go_battle";
                        }), null);

                    }
                    if (strMessage.Substring(0, 8) == "/my_name")
                    {
                        string newUsername = strMessage.Replace("/my_name ", "").Trim('\0');
                        this.dispatcher.Invoke(new Action(() =>
                        {
                            PlayerName = newUsername;
                        }), null);
                    }
                    if (strMessage.Substring(0, 4) == "/Yes")
                    {
                        this.dispatcher.Invoke(new Action(() =>
                        {
                            State = "/Yes";
                        }), null);
                    }
                    if (strMessage.Substring(0, 3) == "/No")
                    {
                        
                        this.dispatcher.Invoke(new Action(() =>
                        {
                            State = "/No";
                        }), null);
                        KillSocket(client.Socket);
                        client.Socket = null;
                    }
                    if (strMessage.Substring(0, 7) == "/Battle")
                    {
                        this.dispatcher.Invoke(new Action(() =>
                        {
                            State = "/Battle";
                        }), null);
                    }
                    if (strMessage.Substring(0, 4) == "/hit")
                    {
                        string xy = strMessage.Replace("/hit", "").Trim('\0');
                        this.dispatcher.Invoke(new Action(() =>
                        {
                            State = "/hit"+xy;
                        }), null);
                    }
                    if (strMessage.Substring(0, 14) == "/AttackResult ")
                    {
                        string AttackResult = strMessage.Replace("/AttackResult ", "").Trim('\0');
                        this.dispatcher.Invoke(new Action(() =>
                        {
                            State = "/AttackResult" + AttackResult;
                        }), null);
                    }

                }
            }
            catch (Exception ex)
            {
                State = "/Error -Receive message fail! " + ex.Message;
            }
        }

        public void SendMessage(string msg)
        {
            SendMessage(client, msg);
        }

        private void SendMessage(Client from, string strMessage)
        {
            try
            {
                client.Socket.Send(Encoding.Unicode.GetBytes(strMessage));
            }
            catch (Exception ex)
            {
                State = "/Error -Send message fail! " + ex.Message;
            }
        }

        public void CreateConnect()
        {
            if (this.IsClientConnected) return;

            client = new Client();
            //PlayerName = "NewUser"; // Временное имя пользователя
            try
            {
                ipEndPoint = new IPEndPoint(ip, port);
                client.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Socket.Connect(ipEndPoint);
                SendMessage("/go_battle " + MyName);
                IsClientConnected = true;
                State = "Сonnected. Awaiting confirmation";
                GetMessage(client); //получаем имя соперника
                //а дальше нужно запустить поток в котором ожидать согласия на начало игры
            }
            catch (Exception ex)
            {
                State = "/Error -Create connection fail! " + ex.Message;
            }
        }

        public void StopConnection()
        {
            KillSocket(client.Socket);
            client.Socket = null;
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
    }
}
