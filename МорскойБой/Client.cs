using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Battleships
{
    class Client
    {
        public string PlayerName
        {
            get;
            set;
        }
        public IPAddress PlayerIP
        {
            get;
            set;
        }
        public Socket Socket
        {
            get;
            set;
        }
        public Thread Thread
        {
            get;
            set;
        }
        public bool IsSocketConnected()
        {
            if (!Socket.Connected)
                return false;

            return true;
        }

       
    }
}
