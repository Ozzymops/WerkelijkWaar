using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class AdminModel
    {
        public List<Classes.User> UserList { get; set; }
        public Classes.User User { get; set; }
        public List<Classes.Story> StoryList { get; set; }
        public Classes.Story Story { get; set; }
    }
}
