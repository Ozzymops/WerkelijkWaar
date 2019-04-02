using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class LoginModel
    {
        // Navigation
        /// <summary>
        /// 0: administratieve login;
        /// 1: game login.
        /// </summary>
        public int Screen { get; set; }

        /// <summary>
        /// Only used if Screen == 0.
        /// 0: configuration;
        /// 1: inzage.
        /// </summary>
        public int Destination { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }
    }
}
