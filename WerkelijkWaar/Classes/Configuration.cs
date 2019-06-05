using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class Configuration
    {
        /// <summary>
        /// Configuration ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Owner's User ID
        /// </summary>
        public int OwnerId { get; set; }

        /// <summary>
        /// Maximum time in seconds for the writing phase. 600 (10:00) by default
        /// </summary>
        public int MaxWritingTime { get; set; }
        
        /// <summary>
        /// Maximum time in seconds for the reading phase. 900 (15:00) by default
        /// </summary>
        public int MaxReadingTime { get; set; }

        /// <summary>
        /// Gained followers per correct answer. 5 by default
        /// </summary>
        public int FollowerGain { get; set; }

        /// <summary>
        /// Lost followers per incorrect answer. 5 by default
        /// </summary>
        public int FollowerLoss { get; set; }

        /// <summary>
        /// Gained followers per vote. 1 by default
        /// </summary>
        public int FollowerPerVote { get; set; }

        /// <summary>
        /// Gained cash per follower per round. 1.00 by default
        /// </summary>
        public double CashPerFollower { get; set; }

        /// <summary>
        /// Gained cash per vote. 0.50 by default
        /// </summary>
        public double CashPerVote { get; set; }

        /// <summary>
        /// Maximum amount of allowed players. 30 by default
        /// </summary>
        public int MaxPlayers { get; set; }

        /// <summary>
        /// Are power-ups allowed during gameplay? True by default
        /// </summary>
        public bool PowerupsAllowed { get; set; }

        /// <summary>
        /// Power-up cost multiplier. 1.0x by default
        /// </summary>
        public double PowerupsCostMult { get; set; }
    }
}
