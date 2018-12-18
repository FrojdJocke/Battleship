using Caliburn.Micro;
using SinkMyBattleship_2._0.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SinkMyBattleship_2._0.ViewModels
{
    public class ShellViewModel : INotifyPropertyChanged
    {
        private int _carrierRow;
        private int _carrierColumn;
        private int _carrierColumnSpan;
        private int _carrierRowSpan;
        private bool _carrierHorizontal;

        public ShellViewModel()
        {

            Boats.Add(new Boat("Carrier", new Dictionary<string, bool>() { { "A1", false }, { "A2", false }, { "A3", false }, { "A4", false }, { "A5", false } }));
            Boats.Add(new Boat("Battleship", new Dictionary<string, bool>() { { "B1", false }, { "B2", false }, { "B3", false }, { "B4", false } }));
            Boats.Add(new Boat("Destroyer", new Dictionary<string, bool>() { { "C1", false }, { "C2", false }, { "C3", false } }));
            Boats.Add(new Boat("Submarine", new Dictionary<string, bool>() { { "D1", false }, { "D2", false }, { "D3", false } }));
            Boats.Add(new Boat("Patrol Boat", new Dictionary<string, bool>() { { "E1", false }, { "E2", false } }));

            CarrierRow = 1;
            CarrierColumn = 1;
            CarrierHorizontal = true;
        }

        public string Name { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }

        public Player Player { get; set; }

        public List<Boat> Boats { get; set; } = new List<Boat>();

        public int CarrierRow
        {
            get => _carrierRow;
            set
            {
                _carrierRow = value;
                OnPropertyChanged(nameof(CarrierRow));
            }
        }

        public int CarrierColumn
        {
            get => _carrierColumn;
            set
            {
                _carrierColumn = value;
                OnPropertyChanged(nameof(CarrierColumn));
            }
        }

        public int CarrierColumnSpan
        {
            get => _carrierColumnSpan;
            set
            {
                _carrierColumnSpan = value;
                OnPropertyChanged(nameof(CarrierColumnSpan));
            }
        }

        public int CarrierRowSpan
        {
            get => _carrierRowSpan;
            set
            {
                _carrierRowSpan = value;
                OnPropertyChanged(nameof(CarrierRowSpan));
            }
        }

        public bool CarrierHorizontal
        {
            get => _carrierHorizontal;
            set
            {
                _carrierHorizontal = value;
                if (_carrierHorizontal)
                {
                    CarrierRowSpan = 1;
                    CarrierColumnSpan = 5;
                }
                else
                {
                    CarrierRowSpan = 5;
                    CarrierColumnSpan = 1;
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void PlayGame()
        {
            
            var manager = new WindowManager();
            manager.ShowWindow(new MainViewModel(new Player(Name, Address, Port, Boats)), null);
            Application.Current.Windows[0].Close();

            

        }

        private int GetRow()
        {
            return 1;
        }
    }
}
