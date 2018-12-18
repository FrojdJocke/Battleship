using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinkMyBattleship_2._0.Models
{
    public class Board : INotifyPropertyChanged
    {
        public Board()
        {
            Coor = new Dictionary<string, int>();

            InitBoard();
        }

        public void InitBoard()
        {
            for (int i = 1; i <= 10; i++)
            {
                for (int j = 1; j <= 10; j++)
                {
                    Coor.Add(new Position("A1", 1).GetCoordinateFrom(i, j), 0);
                }
            }
        }
        // 0 == not fired at, 1= hit, 2 = miss
        private Dictionary<string, int> _coor;

        public Dictionary<string, int> Coor
        {
            get { return _coor; }
            set
            {
                _coor = new Dictionary<string, int>();
                _coor = value;
                OnPropertyChanged(nameof(Coor));

            }
        }


        //private SolidColorBrush _A1;

        //public SolidColorBrush A1
        //{
        //    get { return _A1; }
        //    set
        //    {
        //        switch (Coor["A1"])
        //        {
        //            case 0:
        //                _A1 = Brushes.LightBlue;
        //                break;
        //            case 1:
        //                _A1 = Brushes.Red;
        //                break;
        //            case 2:
        //                _A1 = Brushes.White;
        //                break;
        //            default:
        //                break;
        //        }

        //        OnPropertyChanged(nameof(A1));
        //    }
        //}


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
