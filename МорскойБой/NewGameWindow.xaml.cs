using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Battleships
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class NewGameWindow : Window
    {
        private string IP_;
        private string PlayerName_;
        public string IP { get; set; }
        public string PlayerName
        {
            get { return PlayerName_; }
            set {
                PlayerName_ = value;
                if (String.IsNullOrEmpty(PlayerName_))
                    PlayerName_ = System.Net.Dns.GetHostName();
                    }
        }
        public NewGameWindow(string ip="", string playername="")
        {
            IP = ip;
            PlayerName = playername;
            InitializeComponent();
            tb_playername.Text = PlayerName;
            tb_ip.Text = IP;
            
        }
    }
}
