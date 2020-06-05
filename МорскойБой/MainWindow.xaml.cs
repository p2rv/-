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
    public class Cell : Button
    {
        private bool isdesk = false;
        private bool isdamage = false;

        public bool IsDesk
        {
            get { return isdesk; }
            set
            {
                isdesk = value;
                this.Background = Brushes.Transparent;
                if (isdesk)
                    this.Background = Brushes.Green;
            }
        }
        public bool IsFree
        {
            get { return !isdesk; }
        }
        public bool IsDamage
        {
            get { return isdamage; }
            set
            {
                isdamage = value;
                if (isdamage)
                {
                    if (isdesk)
                    {
                        Content = "X";
                        Foreground = Brushes.Red;
                        FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        Content = "o";
                        Foreground = Brushes.SlateGray;
                        FontWeight = FontWeights.Bold;
                    }
                }
                else
                {
                    Content = "";
                    Foreground = Brushes.Transparent;
                    FontWeight = FontWeights.Bold;
                }
            }
        }
    }

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
                mynet.WaitMessage();
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
            if(tb_statusbar.Text == "/go_batle")
            {
                MessageBoxResult result = MessageBox.Show(this,"Вооу, Нам кинули вызов. Начнем бой?", "Запрос на начало игры", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                    mynet.SendMessage("/Yes");
                else
                {
                    mynet.SendMessage("/No");
                    mynet.StopConnection();
                }
            }
            if (tb_statusbar.Text == "/Yes")
               MessageBox.Show(this, "Противник принял наш вызов. Теперь необходимо расставить карабли по карте. Если вы забыли правила игры загляните в раздел справки", "В бойййй!", MessageBoxButton.OK);
            if (tb_statusbar.Text == "/No")
                MessageBox.Show(this, "Противник не принял наш вызов. Засчитываем это как техническое поражение!", "Противник струсил", MessageBoxButton.OK);
        }
        ~MainWindow()
        {
            if (mynet.IsServerActive)
                mynet.StopServer();

        }
    }
}
