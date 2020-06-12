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
    
    public enum GameResult
    {
        Win,
        Loss
    }

    public enum AttackResult
    {
        Hit,
        Miss,
        Destroy,
        Win,
        Loss
    }

    public enum GameStage
    {
        NotStarted,     //старт
        ArrangingShips, //растановка кораблей
        Battle,         //бой
        Finished        //завершение
    }

    public enum ShipState
    {
        Undamaged,
        Damaged,
        Sunk
    }

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

    public class Ship
    {
        private List<Cell> shipCell=new List<Cell>();

        public void SetDesk(Cell _cell)
        {
            _cell.IsDesk = true;
            shipCell.Add(_cell);
        }
        
        public int Size
        {
            get { return shipCell.Count; }
        }

        public ShipState State
        {
            get
            {
                int damageSize = 0;
                foreach(Cell cell in shipCell)
                    if (cell.IsDamage)
                        damageSize++;

                if (damageSize == 0)
                    return ShipState.Undamaged;

                if (damageSize < this.Size)
                    return ShipState.Damaged;

                return ShipState.Sunk;
            }
        }

        public AttackResult Attack(Cell _attackedCell)
        {
            if (shipCell.Contains(_attackedCell))
            {
                _attackedCell.IsDamage = true;
                switch (State)
                {
                    case ShipState.Damaged:
                        return AttackResult.Hit;
                    case ShipState.Sunk:
                        foreach (Cell cell in shipCell)
                            cell.Background = Brushes.DarkRed;
                        return AttackResult.Destroy;
                }
            }
            return AttackResult.Loss;
        }

        public void Clear()
        {
            foreach (Cell cell in shipCell)
                cell.IsDesk = false;
        }

    }

    public class Unallocated
    {
        public int x4;
        public int x3;
        public int x2;
        public int x1;

        public Unallocated()
        {
            x4 = 1;
            x3 = 2;
            x2 = 3;
            x1 = 4;
        }

        public void Clear()
        {
            x4 = 1;
            x3 = 2;
            x2 = 3;
            x1 = 4;
        }
    }

    public partial class MainWindow : Window
    {
        private GameStage gameStage; //TODO сделать изменение через свойство, в свойстве активировать кнопки 
        private Network mynet;
        private string player1_name;    //ваше имя 

        private Cell[] myButtons      = new Cell[100];
        private Cell[] enemyButtons   = new Cell[100];
        private List<Ship> myShips    = new List<Ship>();
        private List<Ship> enemyShips = new List<Ship>();
        private Unallocated uaShips   = new Unallocated();

        private int shipSize;
        private List<Cell> curShip = new List<Cell>(0);

        public MainWindow()
        {
            InitializeComponent();
            mynet = new Network();
            Player1_name = "loser006";
            this.DataContext = mynet;

            mynet.StartServer();

            FillField();
            curShip = new List<Cell>();
            shipSize = 4;
            gameStage = GameStage.ArrangingShips;
            FillUnallocatedShip();
        }

        private void NewGame(object sender, RoutedEventArgs e)
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
      

        private void FillUnallocatedShip()
        {
            rb_1.IsEnabled = true;
            rb_2.IsEnabled = true;
            rb_3.IsEnabled = true;
            rb_4.IsEnabled = true;

            tb_ship1.Text = uaShips.x1.ToString();
            if (uaShips.x1 == 0)
            {
                rb_1.IsEnabled = false;
                if (rb_1.IsChecked.Value && uaShips.x4 != 0)
                    rb_4.IsChecked = true;
                if (rb_1.IsChecked.Value && uaShips.x3 != 0)
                    rb_3.IsChecked = true;
                if (rb_1.IsChecked.Value && uaShips.x2 != 0)
                    rb_2.IsChecked = true;
            }
            tb_ship2.Text = uaShips.x2.ToString();
            if (uaShips.x2 == 0)
            {
                rb_2.IsEnabled = false;
                if (rb_2.IsChecked.Value && uaShips.x4 != 0)
                    rb_4.IsChecked = true;
                if (rb_2.IsChecked.Value && uaShips.x3 != 0)
                    rb_3.IsChecked = true;
                if (rb_2.IsChecked.Value && uaShips.x1 != 0)
                    rb_1.IsChecked = true;
            }
            tb_ship3.Text = uaShips.x3.ToString();
            if (uaShips.x3 == 0)
            {
                rb_3.IsEnabled = false;
                if (rb_3.IsChecked.Value && uaShips.x4 != 0)
                    rb_4.IsChecked = true;
                if (rb_3.IsChecked.Value && uaShips.x2 != 0)
                    rb_2.IsChecked = true;
                if (rb_3.IsChecked.Value && uaShips.x1 != 0)
                    rb_1.IsChecked = true;
            }
            tb_ship4.Text = uaShips.x4.ToString();
            if (uaShips.x4 == 0)
            {
                rb_4.IsEnabled = false;
                if (rb_4.IsChecked.Value && uaShips.x3 != 0)
                    rb_3.IsChecked = true;
                if (rb_4.IsChecked.Value && uaShips.x2 != 0)
                    rb_2.IsChecked = true;
                if (rb_4.IsChecked.Value && uaShips.x1 != 0)
                    rb_1.IsChecked = true;
            }
        }
        
        private void DecUnallocated()
        {
            if (ShipSize == 4)
                uaShips.x4--;
            if (ShipSize == 3)
                uaShips.x3--;
            if (ShipSize == 2)
                uaShips.x2--;
            if (ShipSize == 1)
                uaShips.x1--;
            FillUnallocatedShip();

            if (uaShips.x1 + uaShips.x2 + uaShips.x3 + uaShips.x4 == 0)
                gameStage = GameStage.Battle;
        }

        private void Bt_goBattle_Click(object sender, RoutedEventArgs e)
        {
            gameStage = GameStage.Battle;
        }

        private void Bt_ClearField_Click(object sender, RoutedEventArgs e)
        {
            foreach (Ship ship in myShips)
                ship.Clear();
            myShips.Clear();
            uaShips.Clear();
            FillUnallocatedShip();
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

        private void FillField()
        {
            for (int row = 0; row <= 9; row++)
            {
                for (int col = 0; col <= 9; col++)
                {
                    Cell b = new Cell();
                    b.Background = Brushes.Transparent;
                    b.Name = "btn" + row + col;
                    b.Focusable = false;

                    myButtons[row * 10 + col] = b;
                    b.Click += new RoutedEventHandler(this.CellClick);
                    Grid.SetColumn(b, col);
                    Grid.SetRow(b, row);
                    gr_myField.Children.Add(b);
                }
            }
        }

        public int ShipSize
        {
            get { return shipSize; }
            set
            {
                if (value >= 1 && value <= 4)
                    shipSize = value;
            }
        }

        private void ArrangeShips(Cell cell)
        {
            if (cell.IsFree && ValidPositioned(cell)) //если ячейка не занята проверяем можно ли расположить карабль заданного размера
            {

                cell.IsDesk = true;
                if (curShip.Count == 0)
                {
                    curShip.Add(cell);
                    if (shipSize == 1)
                    {
                        Ship newShip = new Ship();
                        newShip.SetDesk(cell);
                        curShip.Clear();
                        myShips.Add(newShip);
                        DecUnallocated();
                    }
                }
                else
                {
                    int row_, col_;
                    //вычисляем координаты предыдущей точки
                    row_ = (int)Char.GetNumericValue(curShip[0].Name[3]);
                    col_ = (int)Char.GetNumericValue(curShip[0].Name[4]);

                    int row, col;
                    //вычисляем координаты текущей точки
                    row = (int)Char.GetNumericValue(cell.Name[3]);
                    col = (int)Char.GetNumericValue(cell.Name[4]);

                    Ship newShip = new Ship();
                    if (row_ == row && col_ > col)
                    {
                        for (int i = col_ - (ShipSize - 1); i <= col_; i++)
                            newShip.SetDesk(myButtons[row * 10 + i]);
                    }

                    if (row_ == row && col_ < col)
                    {
                        for (int i = col_; i <= col_ + (ShipSize - 1); i++)
                            newShip.SetDesk(myButtons[row * 10 + i]);
                    }

                    if (col_ == col && row_ > row)
                    {
                        for (int i = row_ - (ShipSize - 1); i <= row_; i++)
                            newShip.SetDesk(myButtons[i * 10 + col]);
                    }

                    if (col_ == col && row_ < row)
                    {
                        for (int i = row_; i <= row_ + (ShipSize - 1); i++)
                            newShip.SetDesk(myButtons[i * 10 + col]);
                    }
                    curShip.Clear();
                    myShips.Add(newShip);
                    DecUnallocated();
                }
            }
        }

        private void CellClick(object sender, RoutedEventArgs e)
        {
            switch(gameStage)
            {
                case GameStage.ArrangingShips:
                    {
                        Cell cell = (Cell)sender;
                        ArrangeShips(cell);
                        break;
                    }
                case GameStage.Battle:
                    {
                        break;
                    }
                case GameStage.Finished:
                    {
                        break;
                    }
                case GameStage.NotStarted:
                    {
                        break;
                    }
            }

        }

        private bool ValidPositioned(Cell currentCell)
        {
            int row, col;
            row = (int)Char.GetNumericValue(currentCell.Name[3]);
            col = (int)Char.GetNumericValue(currentCell.Name[4]);
            if (curShip.Count != 0)
            {
                row = (int)Char.GetNumericValue(curShip[0].Name[3]);
                col = (int)Char.GetNumericValue(curShip[0].Name[4]);
            }

            //проверяем есть ли занятые прилегающие ячейки
            if (!RadiusFree(currentCell))
                return false;

            //1-проверяем есть ли достаточно свободных ячеек левее текущей чтобы установить карабль заданного размера
            bool left = false;
            if (col - (ShipSize - 1) >= 0)
            {
                left = true;
                for (int i = col - (ShipSize - 1); i < col; i++)
                {
                    left = left && myButtons[row * 10 + i].IsFree;
                    if (!RadiusFree(myButtons[row * 10 + i]))
                    {
                        left = false;
                        break;
                    }
                }
            }

            //2-проверяем есть ли достаточно свободных ячеек правее текущей чтобы установить карабль заданного размера
            bool right = false;
            if (col + (ShipSize - 1) <= 9)
            {
                right = true;
                for (int i = col + 1; i <= col + (ShipSize - 1); i++)
                {
                    right = right && myButtons[row * 10 + i].IsFree;
                    if (!RadiusFree(myButtons[row * 10 + i]))
                    {
                        right = false;
                        break;
                    }
                }
            }

            //3-проверяем есть ли достаточно свободных ячеек выше текущей чтобы установить карабль заданного размера
            bool up = false;
            if (row - (ShipSize - 1) >= 0)
            {
                up = true;
                for (int i = row - (ShipSize - 1); i < row; i++)
                {
                    up = up && myButtons[i * 10 + col].IsFree;
                    if (!RadiusFree(myButtons[i * 10 + col]))
                    {
                        up = false;
                        break;
                    }
                }
            }

            //4-проверяем есть ли достаточно свободных ячеек ниже текущей чтобы установить карабль заданного размера
            bool down = false;
            if (row + (ShipSize - 1) <= 9)
            {
                down = true;
                for (int i = row + 1; i <= row + (ShipSize - 1); i++)
                {
                    down = down && myButtons[i * 10 + col].IsFree;
                    if (!RadiusFree(myButtons[i * 10 + col]))
                    {
                        down = false;
                        break;
                    }

                }
            }



            if (curShip.Count > 0)
            {
                int row_, col_;
                row_ = (int)Char.GetNumericValue(currentCell.Name[3]);
                col_ = (int)Char.GetNumericValue(currentCell.Name[4]);

                //определяем направление
                if (row_ == row && col_ < col)
                    return left;
                if (row_ == row && col_ > col)
                    return right;
                if (col_ == col && row_ < row)
                    return up;
                if (col_ == col && row_ > row)
                    return down;

                //располагать карабль по диагонали нельзя
                return false;
            }

            return left || right || up || down;
        }

        private bool RadiusFree(Cell currentCell)
        {
            int row, col;
            row = (int)Char.GetNumericValue(currentCell.Name[3]);
            col = (int)Char.GetNumericValue(currentCell.Name[4]);
            Cell prevCell = null;
            if (curShip.Count > 0)
                prevCell = curShip[0];

            for (int i = (row - 1 >= 0) ? (row - 1) : row; i <= ((row + 1 > 9) ? row : (row + 1)); i++)
            {
                for (int j = (col - 1 >= 0) ? (col - 1) : col; j <= ((col + 1 > 9) ? col : (col + 1)); j++)
                {
                    if (myButtons[i * 10 + j] != prevCell && myButtons[i * 10 + j].IsDesk)
                        return false;
                }
            }
            return true;
        }

        private void Set4(object sender, RoutedEventArgs e)
        {
            ShipSize = 4;
            foreach (Cell cell in curShip)
                cell.IsDesk = false;
            curShip.Clear();
        }
        private void Set3(object sender, RoutedEventArgs e)
        {
            ShipSize = 3;
            foreach (Cell cell in curShip)
                cell.IsDesk = false;
            curShip.Clear();
        }
        private void Set2(object sender, RoutedEventArgs e)
        {
            ShipSize = 2;
            foreach (Cell cell in curShip)
                cell.IsDesk = false;
            curShip.Clear();
        }
        private void Set1(object sender, RoutedEventArgs e)
        {
            ShipSize = 1;
            foreach (Cell cell in curShip)
                cell.IsDesk = false;
            curShip.Clear();
        }


        ~MainWindow()
        {
            if (mynet.IsServerActive)
                mynet.StopServer();

        }

      
    }
}
