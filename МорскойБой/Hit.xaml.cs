using System.Windows.Controls;

namespace Battleships
{
    public partial class Hit : UserControl
    {
        int my = 0;
        int enemy = 0;

        public int My
        {
            get { return my; }
            set { my = value; tb_Hits.Text = my.ToString(); }
        }
        public int Enemy
        {
            get { return enemy; }
            set { enemy = value; tb_Hits_my.Text = enemy.ToString(); }
        }

        public Hit()
        {
            InitializeComponent();
        }

    }
}
