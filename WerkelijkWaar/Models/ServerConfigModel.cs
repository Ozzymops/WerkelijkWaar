using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class ServerConfigModel
    {
        /// <summary>
        /// Database connection string
        /// </summary>
        public string DbConnectionString { get; set; }

        /// <summary>
        /// Database username
        /// </summary>
        public string DbUsername { get; set; }

        /// <summary>
        /// Database password
        /// </summary>
        public string DbPassword { get; set; }
    }
}
