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
        private List<int> GetAddressBytes(string ip_str)
        {
            List<int> ip_octets=new List<int>();
            string[] ip = ip_str.Split(new char[] { '.' });
            foreach(string octet in ip)
            {
                int val;
                if (Int32.TryParse(octet, out val))
                    ip_octets.Add(val);
            }
            return ip_octets;
        }
        private void Tb_ip_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int val;
            List<int> ip_octets;
            if (!Int32.TryParse(e.Text, out val) && e.Text != ".")
            {
                e.Handled = true; // отклоняем ввод
                return;
            }
            ip_octets = GetAddressBytes(tb_ip.Text+e.Text);
            if (ip_octets.Last() > 255)
            {
                e.Handled = true;
                return;
            }
           

        }

        private void Tb_ip_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true; // если пробел, отклоняем ввод
            }
        }

        private void Tb_ip_TextChanged(object sender, TextChangedEventArgs e)
        {
            //List<byte> ip_octets;
            //ip_octets = GetAddressBytes(tb_ip.Text);
            //if (ip_octets.Last() >= 100 && tb_ip.Text.Last()!='.')
            //    tb_ip.Text +=  ".";
        }
    }
}
