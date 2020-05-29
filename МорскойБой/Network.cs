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
        private CancellationTokenSource currentOperationToken;
        private int port;
        private IPAddress ip;
        private IPEndPoint ipEndPoint;
        private Socket listenSocket;
        private Socket connection;
        private Client client;
        private string myName;

        public Network(string _myName="")
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

        public bool Send(string msg)
        {
            if (ipEndPoint == null)
                ipEndPoint = new IPEndPoint(ip, port);
            try
            {
                if (listenSocket == null)
                {
                    listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    listenSocket.Connect(ipEndPoint);
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

                //CancellationTokenSource - специальный механизм который позволяет отправить в паралельно выполняющийся поток сигнал о заверешении этого потока
                currentOperationToken = new CancellationTokenSource();
                await Task.Run(() => this.WaitForConnections(currentOperationToken.Token)); //WaitForConnections (ожидание подключений) запускаем в паралельном потоке чтобы не зависал интерфейс
               

                this.IsServerActive = true;
                State = "Server is active. Wait connection";
            }catch(Exception ex)
            {
                //что то пошло не так
                //подчищаем хвосты
                StopServer();
                //сообщаем об ошибке
                State = "/Error -Start server fail! " + ex.Message+"Source: "+ex.Source;
            }
        }

        public void StopServer()
        {
            if (this.thread != null)
            {
                currentOperationToken.Cancel();
            }

            try
            {
                if (listenSocket != null)
                    listenSocket.Shutdown(SocketShutdown.Both);
            }
            catch(Exception)
            {
                listenSocket.Close();
            }
            
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
                    GetHelloMsg(client);
                    SendMessage(client, "/my_name " + myName);
                }
                catch (Exception ex)
                {
                    State = "/Error -Accept connection fail! " + ex.Message;
                }
                if (token.IsCancellationRequested)
                    return;
            }
        }

        private void GetHelloMsg(Client client)
        {
            try
            {
                byte[] inf = new byte[1024];
                int x = client.Socket.Receive(inf);
                if (x > 0)
                {
                    string strMessage = Encoding.Unicode.GetString(inf);
                    // check and execute commands
                    if (strMessage.Substring(0, 9) == "/go_batle")
                    {
                        string newUsername = strMessage.Replace("/go_batle ", "").Trim('\0');


                        this.dispatcher.Invoke(new Action(() =>
                        {
                            PlayerName = newUsername;
                            State = "/go_batle "+ client.PlayerName;
                        }), null);

                    }
                    if(strMessage.Substring(0, 8) == "/my_name")
                    {
                        string newUsername = strMessage.Replace("/my_name ", "").Trim('\0');
                        this.dispatcher.Invoke(new Action(() =>
                        {
                            PlayerName = newUsername;
                        }), null);
                    }

                }
            }
            catch (Exception ex)
            {
                State = "/Error -Receive message fail! " + ex.Message;
            }
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
            if (this.IsClientConnected)  return;

            client = new Client();
            //PlayerName = "NewUser"; // Временное имя пользователя
            try
            {
                ipEndPoint = new IPEndPoint(ip, port);
                client.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Socket.Connect(ipEndPoint);
                SendMessage(client, "/go_batle "+MyName);
                IsClientConnected = true;
                State = "Сonnected. Awaiting confirmation";
                GetHelloMsg(client); //получаем имя соперника
                //а дальше нужно запустить поток в котором ожидать согласия на начало игры
            }
            catch (Exception ex)
            {
                State = "/Error -Create connection fail! " + ex.Message;
            }
        }
 


        void ProcessMessages(Client c)
        {
            //while (true)
            //{
            //    try
            //    {
            //        if (!c.IsSocketConnected())
            //        {
            //            this.dispatcher.Invoke(new Action(() =>
            //            {
            //                Status = "#Обрыв соединения";
            //            }), null);

            //            return;
            //        }

            //        byte[] inf = new byte[1024];
            //        int x = c.Socket.Receive(inf);
            //        if (x > 0)
            //        {
            //            string strMessage = Encoding.Unicode.GetString(inf);
            //            // check and execute commands
            //            if (strMessage.Substring(0, 8) == "/setname")
            //            {
            //                string newUsername = strMessage.Replace("/setname ", "").Trim('\0');

            //                c.Username = newUsername;
            //            }
            //            else if (strMessage.Substring(0, 6) == "/msgto")
            //            {
            //                string data = strMessage.Replace("/msgto ", "").Trim('\0');
            //                string targetUsername = data.Substring(0, data.IndexOf(':'));
            //                string message = data.Substring(data.IndexOf(':') + 1);

            //                this._dispatcher.Invoke(new Action(() =>
            //                {
            //                    SendMessage(c, targetUsername, message);
            //                }), null);
            //            }

            //        }
            //    }
            //    catch (Exception)
            //    {
            //        this._dispatcher.Invoke(new Action(() =>
            //        {
            //            lstClients.Remove(c);
            //            c.Dispose();
            //        }), null);
            //        return;
            //    }
            //}
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
