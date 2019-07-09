﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class ConnectionManager
    {
        /// <summary>
        /// List of Connections
        /// </summary>
        public List<string> Connections { get; set; } = new List<string>();

        /// <summary>
        /// List of Rooms
        /// </summary>
        public List<Classes.Room> Rooms { get; set; } = new List<Classes.Room>();
    }
}
