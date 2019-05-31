using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class AccountViewModel
    {
        public Classes.User User { get; set; }
        public string StatusString { get; set; }
        public int StatusLocation = -1;
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string NewPasswordConfirmation { get; set; }
    }
}
