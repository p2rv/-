using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
}
