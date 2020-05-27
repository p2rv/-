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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;

namespace Battleships
{
    public partial class MainWindow : Window
    {
        private Network mynet;
        private string player1_name;    //ваше имя 

        public MainWindow()
        {
            InitializeComponent();
            mynet = new Network();
            mynet.StartServer();
            this.DataContext = mynet;
        }

        private void MenuItem_NewGame(object sender, RoutedEventArgs e)
        {
            NewGameWindow ng = new NewGameWindow(mynet.IP, Player1_name);
            if (ng.ShowDialog() == true)
            {
                mynet.SetIP(ng.IP);
                Player1_name = ng.PlayerName;
                Try_connect(mynet.IP);
            }
            else
                tb_statusbar.Text = "Отмена создания новой сетевой игры";
        }

        private string Player1_name
        {
            get { return player1_name; }
            set { lb_player1.Content = "Мое поле (" + value + ")"; player1_name = value; }
        }
      
        public bool Try_connect(string ip)
        {
            tb_statusbar.Text = "соединение с "+ip+" ...";
            mynet.Send("Hellow! My name is " + Player1_name);

            return true;
        }



        //todo добавить обработчик начала новной игры
        private void Tb_statusbar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(tb_statusbar.Text=="#New game#")
            {

            }
        }
    }
}
