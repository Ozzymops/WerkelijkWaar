using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class Connection
    {
        public string SocketId { get; set; }
        public bool Pinged { get; set; }
        public int Timeouts { get; set; }
    }
}
