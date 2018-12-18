using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinkMyBattleship_2._0.Models
{
    public class Boat
    {
        public string Name { get; set; }

        public Dictionary<string,bool> Coordinates { get; set; } = new Dictionary<string, bool>();

        public bool Alive { get; set; }
        public Position Position { get; set; }

        public Boat(string name, Dictionary<string,bool> coordinates)
        {
            Alive = true;
            Name = name;
            Coordinates = coordinates;

            Position = new Position(Coordinates.First().Key, coordinates.Count);
            Position.Horizontal = true;
        }
    }
}
