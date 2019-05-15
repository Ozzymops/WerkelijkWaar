using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar
{
    public class GameManager
    {
        public List<Classes.Connection> Connections { get; set; } = new List<Classes.Connection>();
        public List<Classes.Room> Rooms { get; set; } = new List<Classes.Room>();
    }
}
