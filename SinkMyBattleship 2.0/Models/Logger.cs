using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinkMyBattleship_2._0.Models
{
    public class Logger : INotifyPropertyChanged
    {
        private string _log;
        private List<string> _logList;

        public Logger()
        {
            LogList = new List<string>();
        }

        public string Log
        {
            get => _log;
            set
            {
                _log = value;
                OnPropertyChanged(nameof(Log));
            }
        }
        public List<string> LogList
        {
            get => _logList;
            set
            {
                _logList = value;
                if (_logList.Count != 0)
                {
                    Log += $"{_logList.Last()}\r\n";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddToLog(string text)
        {
            LogList.Add(text);
            LogList = LogList;
        }

        public void ClearLog()
        {
            Log = "";
            LogList = new List<string>();
        }
    }
}
