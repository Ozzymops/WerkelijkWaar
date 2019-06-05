using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class ConfigModel
    {
        /// <summary>
        /// Teacher
        /// </summary>
        public Classes.User Teacher { get; set; }

        /// <summary>
        /// Configuration
        /// </summary>
        public Classes.Configuration Config { get; set; }

        /// <summary>
        /// Status string
        /// </summary>
        public string StatusString { get; set; }
    }
}
