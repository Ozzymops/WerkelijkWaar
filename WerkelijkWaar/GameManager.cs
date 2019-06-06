using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar
{
    public class GameManager
    {
        /// <summary>
        /// List of Connections
        /// </summary>
        public List<Classes.Connection> Connections { get; set; } = new List<Classes.Connection>();

        /// <summary>
        /// List of Rooms
        /// </summary>
        public List<Classes.Room> Rooms { get; set; } = new List<Classes.Room>();
    }
}
