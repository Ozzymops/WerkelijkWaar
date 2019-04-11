using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class HubModel
    {
        public Classes.User User { get; set; }
        public List<Classes.User> UserList { get; set; }
    }
}
