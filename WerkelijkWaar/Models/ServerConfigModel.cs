using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class ServerConfigModel
    {
        public string DbConnectionString { get; set; }
        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
    }
}
