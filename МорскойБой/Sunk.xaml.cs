using System.Windows.Controls;

namespace Battleships
{
    public partial class Sunk : UserControl
    {
        int my = 0;
        int enemy = 0;

        public int My
        {
            get { return my; }
            set { my = value; tb_Sunk.Text = my.ToString(); }
        }
        public int Enemy
        {
            get { return enemy; }
            set { enemy = value; tb_Sunk_my.Text = enemy.ToString(); }
        }

        public Sunk()
        {
            InitializeComponent();
        }
    }
}
