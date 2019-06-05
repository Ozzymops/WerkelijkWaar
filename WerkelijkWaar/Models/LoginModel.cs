using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class LoginModel
    {
        /// <summary>
        /// User username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// User password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// User status string
        /// </summary>
        public string Status { get; set; }
    }
}
