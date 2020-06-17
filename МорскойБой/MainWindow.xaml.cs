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
        None,
        Miss,
        Hit,
        Destroy,
        Win,
        Loss
    }

    public enum GameStage
    {
        NotStarted,     //старт
        ArrangingShips, //растановка кораблей
        Battle,         //бой
        Battle_wait,    //бой 
        Finished        //завершение
    }

    public enum ShipState
    {
        Undamaged,
        Damaged,
        Sunk
    }

    public enum WhoseTurn
    {
        None,
        My,
        Enemy
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
        public void UpdateState()
        {
            if(State==ShipState.Sunk)
                foreach (Cell cell in shipCell)
                    cell.Background = Brushes.DarkRed;
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
            return AttackResult.Miss;
        }

        public void Clear()
        {
            foreach (Cell cell in shipCell)
                cell.IsDesk = false;
        }

        public override string ToString()
        {
            string coordinates = "";

            foreach (Cell cell in shipCell)
                coordinates += cell.Name.Substring(3) + ";";

            return coordinates;
        }

    }

    public class Unallocated
    {
        private int x4;
        private int x3;
        private int x2;
        private int x1;

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
            if (ValueChanged != null)
                ValueChanged(this, EventArgs.Empty);
        }

        public event EventHandler ValueChanged;
        public int X4
        {
            get { return x4; }
            set
            {
                if (x4 != value)
                {
                    x4 = value;
                    if (ValueChanged != null)
                        ValueChanged(this, EventArgs.Empty);
                }
            }
        }
        public int X3
        {
            get { return x3; }
            set
            {
                if (x3 != value)
                {
                    x3 = value;
                    if (ValueChanged != null)
                        ValueChanged(this, EventArgs.Empty);
                }
            }
        }
        public int X2
        {
            get { return x2; }
            set
            {
                if (x2 != value)
                {
                    x2 = value;
                    if (ValueChanged != null)
                        ValueChanged(this, EventArgs.Empty);
                }
            }
        }
        public int X1
        {
            get { return x1; }
            set
            {
                if (x1 != value)
                {
                    x1 = value;
                    if (ValueChanged != null)
                        ValueChanged(this, EventArgs.Empty);
                }
            }
        }
    }

    public partial class MainWindow : Window
    {
        private GameStage gameStage; //TODO сделать изменение через свойство, в свойстве активировать кнопки. Также добавить событие изменение состояния 
        private WhoseTurn turn;
        private AttackResult attack;
        private Network mynet;
        private string player1_name;    //ваше имя 

        private Cell[] myButtons      = new Cell[100];
        private Cell[] enemyButtons   = new Cell[100];
        private List<Ship> myShips    = new List<Ship>();
        private List<Ship> enemyShips = new List<Ship>();
        private Unallocated uaShips   = new Unallocated();
        private Cell lastCell;

        private int shipSize;
        private List<Cell> curShip = new List<Cell>(0);

        public MainWindow()
        {

            InitializeComponent();
            FillField();

            mynet = new Network();
            this.DataContext = mynet;
            Player1_name = "loser006";
            mynet.StartServer();

            Turn = WhoseTurn.None;
            uaShips.ValueChanged += UnallocatedChanged;
        }

        

        private void NewGame(object sender, RoutedEventArgs e)
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
        
        private void UnallocatedChanged(object sender, EventArgs e)
        {
            rb_1.IsEnabled = true;
            rb_2.IsEnabled = true;
            rb_3.IsEnabled = true;
            rb_4.IsEnabled = true;

            tb_ship4.Text = uaShips.X4.ToString();
            if (uaShips.X4 == 0)
            {
                rb_4.IsEnabled = false;
                if (rb_4.IsChecked.Value && uaShips.X3 != 0)
                    rb_3.IsChecked = true;
                if (rb_4.IsChecked.Value && uaShips.X2 != 0)
                    rb_2.IsChecked = true;
                if (rb_4.IsChecked.Value && uaShips.X1 != 0)
                    rb_1.IsChecked = true;
            }

            tb_ship3.Text = uaShips.X3.ToString();
            if (uaShips.X3 == 0)
            {
                rb_3.IsEnabled = false;
                if (rb_3.IsChecked.Value && uaShips.X4 != 0)
                    rb_4.IsChecked = true;
                if (rb_3.IsChecked.Value && uaShips.X2 != 0)
                    rb_2.IsChecked = true;
                if (rb_3.IsChecked.Value && uaShips.X1 != 0)
                    rb_1.IsChecked = true;
            }

            tb_ship2.Text = uaShips.X2.ToString();
            if (uaShips.X2 == 0)
            {
                rb_2.IsEnabled = false;
                if (rb_2.IsChecked.Value && uaShips.X4 != 0)
                    rb_4.IsChecked = true;
                if (rb_2.IsChecked.Value && uaShips.X3 != 0)
                    rb_3.IsChecked = true;
                if (rb_2.IsChecked.Value && uaShips.X1 != 0)
                    rb_1.IsChecked = true;
            }

            tb_ship1.Text = uaShips.X1.ToString();
            if (uaShips.X1 == 0)
            {
                rb_1.IsEnabled = false;
                if (rb_1.IsChecked.Value && uaShips.X4 != 0)
                    rb_4.IsChecked = true;
                if (rb_1.IsChecked.Value && uaShips.X3 != 0)
                    rb_3.IsChecked = true;
                if (rb_1.IsChecked.Value && uaShips.X2 != 0)
                    rb_2.IsChecked = true;
            }
        }

        private void DecUnallocated()
        {
            switch (ShipSize)
            {
                case 4:
                    {
                        uaShips.X4--;
                        break;
                    }
                case 3:
                    {
                        uaShips.X3--;
                        break;
                    }
                case 2:
                    {
                        uaShips.X2--;
                        break;
                    }
                case 1:
                    {
                        uaShips.X1--;
                        break;
                    }
            }

            if (uaShips.X1 + uaShips.X2 + uaShips.X3 + uaShips.X4 == 0)
            {
                
                gb_ArrangeShips.IsEnabled = false;
                switch(Turn)
                {
                    case WhoseTurn.None:
                        {
                            Turn = WhoseTurn.My;
                            mynet.SendMessage("/Battle");
                            gameStage = GameStage.Battle_wait;
                            break;
                        }
                    case WhoseTurn.Enemy:
                        {
                            Turn = WhoseTurn.Enemy;
                            mynet.SendMessage("/Battle");
                            gameStage = GameStage.Battle;
                            break;
                        }
                }
                
            }
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
        }

        private Ship AddShip(string coordinates)
        {
            Ship newShip=new Ship();
            int x, y;
            string[] coordinate = coordinates.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string xy in coordinate)
            {
                x = (int)Char.GetNumericValue(xy[0]);
                y = (int)Char.GetNumericValue(xy[1]);
                newShip.SetDesk(enemyButtons[x * 10 + y]);
            }
            return newShip;
        }
        //todo добавить обработчик начала новной игры
        private void Tb_statusbar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(tb_statusbar.Text == "/go_battle")
            {
                MessageBoxResult result = MessageBox.Show(this,"Вооу, Нам кинули вызов. Начнем бой?", "Запрос на начало игры", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    mynet.SendMessage("/Yes");
                    MessageBox.Show(this, "Теперь необходимо расставить карабли по карте. Если вы забыли правила игры загляните в раздел справки", "В бойййй!", MessageBoxButton.OK);
                    gameStage = GameStage.ArrangingShips;
                    gb_ArrangeShips.IsEnabled = true;
                    uaShips.Clear();
                    rb_4.IsChecked = true;
                }
                else
                {
                    mynet.SendMessage("/No");
                    mynet.StopConnection();
                }
            }
            if (tb_statusbar.Text == "/Yes")
            {
                MessageBox.Show(this, "Противник принял наш вызов. Теперь необходимо расставить карабли по карте. Если вы забыли правила игры загляните в раздел справки", "В бойййй!", MessageBoxButton.OK);
                gameStage = GameStage.ArrangingShips;
                gb_ArrangeShips.IsEnabled = true;
                uaShips.Clear();
                rb_4.IsChecked = true;
                tb_statusbar.Text = "Противник принял наш вызов";
            }
            if (tb_statusbar.Text == "/No")
            {
                MessageBox.Show(this, "Противник не принял наш вызов. Засчитываем это как техническое поражение!", "Противник струсил", MessageBoxButton.OK);
                tb_statusbar.Text = "Противник не принял наш вызов";
            }
            if (tb_statusbar.Text == "/Battle")
            {
                if (Turn == WhoseTurn.None)
                {
                    Turn = WhoseTurn.Enemy;
                    tb_statusbar.Text = "Противник уже раставил корабли ";
                }
                else
                {
                    gameStage = GameStage.Battle;
                    Turn = WhoseTurn.My;
                    tb_statusbar.Text = "Противник раставил корабли, Ваш ход!";
                }
            }
            if (tb_statusbar.Text.Length>=4 && tb_statusbar.Text.Substring(0, 4) == "/hit")
            {
                string xy = tb_statusbar.Text.Replace("/hit", "");
                int row, col;
                //вычисляем координаты текущей точки
                row = (int)Char.GetNumericValue(xy[0]);
                col = (int)Char.GetNumericValue(xy[1]);
                Cell cell = myButtons[row * 10 + col];
                cell.IsDamage = true;
                AttackResult attack=AttackResult.None;
                Ship destroyedShip = null;
                foreach (Ship ship in myShips)
                {
                    attack = ship.Attack(cell);
                    if (attack > AttackResult.Miss)
                    {
                        if (attack == AttackResult.Destroy)
                        {
                            destroyedShip = ship;
                            if (AllShipSunk())
                                attack = AttackResult.Win;
                        }
                        break;
                    }
                }
                if (attack == AttackResult.Destroy)
                    mynet.SendMessage("/AttackResult " + ((int)attack).ToString() + "/" + destroyedShip.ToString());
                else
                    mynet.SendMessage("/AttackResult " + ((int)attack).ToString());

                if (attack == AttackResult.Miss)
                    Turn = WhoseTurn.My;

                switch(attack)
                {
                    case AttackResult.Miss:
                        tb_statusbar.Text = "Противник промахнулся, наш ход!)))";
                        break;
                    case AttackResult.Hit:
                        tb_statusbar.Text = "В нас попали!(";
                        break;
                    case AttackResult.Destroy:
                        tb_statusbar.Text = "Наш корабль затоплен!((";
                        break;
                    case AttackResult.Win:
                        tb_statusbar.Text = "Мы проиграли!(((";
                        gameStage = GameStage.Finished;
                        MessageBox.Show("You are lose!", "Поражение");
                        break;
                }
            }
            if (tb_statusbar.Text.Length >= 14 && tb_statusbar.Text.Substring(0, 14) =="/AttackResult ")
            {

                int result = Convert.ToInt32(tb_statusbar.Text.Substring(14,1));
                AttackResult attack = (AttackResult)result;
                switch(attack)
                {
                    case AttackResult.Miss:
                        lastCell.IsDamage = true;
                        Turn = WhoseTurn.Enemy;
                        tb_statusbar.Text = "Промах, ход переходит к противнику";
                        break;
                    case AttackResult.Hit:
                        lastCell.IsDesk = true;
                        lastCell.IsDamage = true;
                        Turn = WhoseTurn.My;
                        tb_statusbar.Text = "Есть попадание";
                        break;
                    case AttackResult.Destroy:
                        lastCell.IsDesk = true;
                        lastCell.IsDamage = true;
                        Turn = WhoseTurn.My;
                        string coordinates = tb_statusbar.Text.Substring(16);
                        Ship destroyedShip = AddShip(coordinates);
                        enemyShips.Add(destroyedShip);
                        destroyedShip.UpdateState();
                        tb_statusbar.Text = "Затопил";
                        break;
                    case AttackResult.Win:
                        gameStage = GameStage.Finished;
                        MessageBox.Show("You are win!", "Победа");
                        tb_statusbar.Text = "Победа";
                        break;
                }

            }


            }

        private bool AllShipSunk()
        {
            foreach (Ship ship in myShips)
                if (ship.State != ShipState.Sunk)
                    return false;
            return true;
        }
        private WhoseTurn Turn
        {
            get { return turn; }
            set
            {
                if (turn != value)
                    turn = value;
                if (turn == WhoseTurn.My && gameStage == GameStage.Battle)
                    gr_enemyField.IsEnabled = true;
                else
                    gr_enemyField.IsEnabled = false;
            }
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
                    b.Click += new RoutedEventHandler(this.MyFieldCellClick);
                    Grid.SetColumn(b, col);
                    Grid.SetRow(b, row);
                    gr_myField.Children.Add(b);
                }
            }
            for (int row = 0; row <= 9; row++)
            {
                for (int col = 0; col <= 9; col++)
                {
                    Cell b = new Cell();
                    b.Background = Brushes.Transparent;
                    b.Name = "btn" + row + col;
                    b.Focusable = false;

                    enemyButtons[row * 10 + col] = b;
                    b.Click += new RoutedEventHandler(this.EnemyCellClick);
                    Grid.SetColumn(b, col);
                    Grid.SetRow(b, row);
                    gr_enemyField.Children.Add(b);
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

                
                if (curShip.Count == 0)
                {
                    cell.IsDesk = true;
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

        private void MyFieldCellClick(object sender, RoutedEventArgs e)
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

        private void AttackCell(Cell cell)
        {
            if(!cell.IsDamage)
            {
                int row, col;
                //вычисляем координаты текущей точки
                row = (int)Char.GetNumericValue(cell.Name[3]);
                col = (int)Char.GetNumericValue(cell.Name[4]);

                mynet.SendMessage("/hit"+row.ToString()+col.ToString());
                turn = WhoseTurn.Enemy;

            }
        }

        private void EnemyCellClick(object sender, RoutedEventArgs e)
        {
            switch (gameStage)
            {
                case GameStage.ArrangingShips:
                    {
                        MessageBox.Show("Игра еще не началась. Для начала на своем поле раставьте кормабли");
                        break;
                    }
                case GameStage.Battle_wait:
                    {
                        MessageBox.Show("Противник еще не расставил свои корабли по игровому полю", "Игра еще не началась");
                        break;
                    }
                case GameStage.Battle:
                    {
                        if(turn==WhoseTurn.My)
                        {
                            lastCell = (Cell)sender;
                            AttackCell(lastCell);
                        }
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

        private void Tb_player1_TextChanged(object sender, TextChangedEventArgs e)
        {
            Player1_name = tb_player1.Text;
        }
    }
}
