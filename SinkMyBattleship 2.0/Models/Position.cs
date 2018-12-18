using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinkMyBattleship_2._0.Models
{
    public class Position : INotifyPropertyChanged
    {
        private readonly int length;
        private int _Row;
        private int _Column;
        private int _ColumnSpan;
        private int _RowSpan;
        private bool _Horizontal;


        public Position(string firstCoordinate, int length)
        {
            Column = int.Parse(firstCoordinate[1].ToString());

            // check for column 10 as well
            switch (firstCoordinate[0].ToString())
            {
                case "A":
                    Row = 1;
                    break;
                case "B":
                    Row = 2;
                    break;
                case "C":
                    Row = 3;
                    break;
                case "D":
                    Row = 4;
                    break;
                case "E":
                    Row = 5;
                    break;
                case "F":
                    Row = 6;
                    break;
                case "G":
                    Row = 7;
                    break;
                case "H":
                    Row = 8;
                    break;
                case "I":
                    Row = 9;
                    break;
                case "J":
                    Row = 10;
                    break;
                default:
                    break;
            }

            this.length = length;
        }

        public int Row
        {
            get => _Row;
            set
            {
                _Row = value;
                OnPropertyChanged(nameof(Row));
            }
        }

        public int Column
        {
            get => _Column;
            set
            {
                _Column = value;
                OnPropertyChanged(nameof(Column));
            }
        }

        public int ColumnSpan
        {
            get => _ColumnSpan;
            set
            {
                _ColumnSpan = value;
                OnPropertyChanged(nameof(ColumnSpan));
            }
        }

        public int RowSpan
        {
            get => _RowSpan;
            set
            {
                _RowSpan = value;
                OnPropertyChanged(nameof(RowSpan));
            }
        }

        public bool Horizontal
        {
            get => _Horizontal;
            set
            {
                _Horizontal = value;
                if (_Horizontal)
                {
                    RowSpan = 1;
                    ColumnSpan = length;
                }
                else
                {
                    RowSpan = length;
                    ColumnSpan = 1;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string GetCoordinateFrom(int row, int column)
        {
            var coordinate = "";
            switch (row)
            {
                case 1:
                    coordinate += "A";
                    break;
                case 2:
                    coordinate += "B";
                    break;
                case 3:
                    coordinate += "C";
                    break;
                case 4:
                    coordinate += "D";
                    break;
                case 5:
                    coordinate += "E";
                    break;
                case 6:
                    coordinate += "F";
                    break;
                case 7:
                    coordinate += "G";
                    break;
                case 8:
                    coordinate += "H";
                    break;
                case 9:
                    coordinate += "I";
                    break;
                case 10:
                    coordinate += "J";
                    break;
                default:
                    break;
            }

            coordinate += column;

            return coordinate;
        }

    }
}
