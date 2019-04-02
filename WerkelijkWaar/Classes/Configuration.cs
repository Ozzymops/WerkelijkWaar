using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class Configuration
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public int MaxWritingTime { get; set; }
        public int MaxReadingTime { get; set; }
        public int FollowerGain { get; set; }
        public int FollowerLoss { get; set; }
        public int FollowerPerVote { get; set; }
        public double CashPerFollower { get; set; }
        public double CashPerVote { get; set; }
        public int MaxPlayers { get; set; }
        public bool PowerupsAllowed { get; set; }
        public double PowerupsCostMult { get; set; }
    }
}
