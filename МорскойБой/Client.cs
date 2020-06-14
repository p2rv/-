using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.ComponentModel;

namespace Battleships
{
    public class Client : IDisposable, INotifyPropertyChanged
    {
        private int id;

        string playerName;
        public string PlayerName
        {
            get { return playerName; }
            set
            {
                playerName = value;
                this.NotifyPropertyChanged("PlayerName");
            }
        }
        public int ID
        {
            get
            {
                return id;
            }
            set
            {
                this.id = value;
                this.NotifyPropertyChanged("ID");
            }
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


        #region IDisposable implementation
        private bool _isDisposed = false;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (this.Socket != null)
                {
                    this.Socket.Shutdown(SocketShutdown.Both);
                    this.Socket.Dispose();
                    this.Socket = null;
                }
                if (this.Thread != null)
                    this.Thread = null;
                _isDisposed = true;
            }
        }
        #endregion
        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        #endregion


    }
}
