using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class ConfigModel
    {
        public Classes.User Teacher { get; set; }
        public Classes.Configuration Config { get; set; }
        public string StatusString { get; set; }
    }
}
