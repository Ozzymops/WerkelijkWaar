using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class User
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int Group { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int LoginAttempts { get; set; }
        public string ImageSource { get; set; }

        // Other
        public double AverageScore { get; set; }
    }
}
