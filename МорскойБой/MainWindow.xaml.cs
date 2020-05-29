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
            Player1_name = "loser006";
            this.DataContext = mynet;

            mynet.StartServer();
        }

        private void MenuItem_NewGame(object sender, RoutedEventArgs e)
        {
            NewGameWindow ng = new NewGameWindow(mynet.IP, Player1_name);
            if (ng.ShowDialog() == true)
            {
                mynet.SetIP(ng.IP);
                Player1_name = ng.PlayerName;
                mynet.CreateConnect();
            }
            else
                tb_statusbar.Text = "Отмена создания новой сетевой игры";
        }

        private string Player1_name
        {
            get { return player1_name; }
            set { player1_name = value; mynet.MyName = value; }
        }
      



        //todo добавить обработчик начала новной игры
        private void Tb_statusbar_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if(tb_statusbar.Text.Substring(0, 9) == "/go_batle")
            //{
            //    MessageBoxResult result = MessageBox.Show("Вооу, Нам кинули вызов", "Запрос на начало игры", MessageBoxButton.YesNo);
            //}
        }
        ~MainWindow()
        {
            if (mynet.IsServerActive)
                mynet.StopServer();

        }
    }
}
