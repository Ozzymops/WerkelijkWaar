using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class LoginModel
    {
        // Navigation
        public string Username { get; set; }
        public string Password { get; set; }
        public string Status { get; set; }
    }
}
