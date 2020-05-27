using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Net;

namespace Battleships
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class NewGameWindow : Window
    {
        public string IP
        {
            get { return tb_ip.Text; }
            set { tb_ip.Text = value; }
        }
        
        public string PlayerName
        {
            get { return tb_playername.Text; }
            set
            {
                tb_playername.Text = value;
                if (String.IsNullOrEmpty(tb_playername.Text))
                    tb_playername.Text = System.Net.Dns.GetHostName();

            }
        }
        public NewGameWindow(string ip="", string playername="")
        {
            
            InitializeComponent();
            PlayerName = playername;
            IP = ip;
            this.ResizeMode = ResizeMode.NoResize;

        }
        private List<int> GetAddressBytes(string ip_str)
        {
            List<int> ip_octets=new List<int>();
            string[] ip = ip_str.Split(new char[] { '.' });
            foreach(string octet in ip)
            {
                if (Int32.TryParse(octet, out int val))
                    ip_octets.Add(val);
            }
            return ip_octets;
        }

        // Валидация ввода ip адреса
        // в текстбокс можно вводить только цифры и точки
        // число в каждом из октетов IP адреса не может превышать 255
        private void Tb_ip_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

            //отклоняем все символы кроме цифр и точки 
            if (!Int32.TryParse(e.Text, out int val) && e.Text != ".")
            {
                e.Handled = true; // отклоняем ввод
                return;
            }


            //если число в последнем октете превышает 255 то отклоняем ввод
            List<int> ip_octets;
            ip_octets = GetAddressBytes(tb_ip.Text+e.Text);
            if (ip_octets.Last() > 255)
            {
                e.Handled = true;
                return;
            }

            //после четвертого октета не должно быть точки
            if (ip_octets.Count == 4 && e.Text==".")
                e.Handled = true;

        }

        // Валидация ввода ip адреса
        //
        private void Tb_ip_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // если пробел, отклоняем ввод
            if (e.Key == Key.Space)
            {
                e.Handled = true; 
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {

            this.DialogResult = true;
        }
    }
}
