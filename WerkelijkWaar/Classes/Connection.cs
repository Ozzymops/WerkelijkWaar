using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class Connection
    {
        /// <summary>
        /// Unique ID for identification. Randomly generates upon (re)connecting
        /// </summary>
        public string SocketId { get; set; }

        /// <summary>
        /// Did the client send a ping to the server yet?
        /// </summary>
        public bool Pinged { get; set; }

        /// <summary>
        /// Amount of times the client timed out
        /// </summary>
        public int Timeouts { get; set; }
    }
}
