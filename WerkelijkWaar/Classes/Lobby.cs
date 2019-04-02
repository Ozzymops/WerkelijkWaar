using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class Lobby
    {
        public int Id { get; set; }

        // from GenerateCode()
        public int Code { get; set; }
        private List<User> Players { get; set; }
        private List<Story> Stories { get; set; }
        private List<List<User>> PlayerGroups { get; set; }
        private Configuration Configuration { get; set; }

        // todo
        public int GenerateCode()
        {
            return 0;
        }
    }
}
