using System.Windows;
using System.Windows.Controls;

namespace Battleships
{
    public class Score : TextBlock
    {
        int my = 0;
        int enemy = 0;
        public int My
        {
            get { return my; }
            set { my = value; Text = my.ToString() + " : " + enemy.ToString(); }
        }
        public int Enemy
        {
            get { return enemy; }
            set { enemy = value; Text = my.ToString() + " : " + enemy.ToString(); }
        }
    }
}
