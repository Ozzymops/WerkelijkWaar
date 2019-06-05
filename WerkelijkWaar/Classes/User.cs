using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class User
    {
        /// <summary>
        /// User ID
        /// </summary>
        public int Id { get; set; }    

        /// <summary>
        /// Role ID
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// Group ID
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User surname
        /// </summary>
        public string Surname { get; set; }

        /// <summary>
        /// User username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// User password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// User log-in attempts
        /// </summary>
        public int LoginAttempts { get; set; }

        /// <summary>
        /// User's avatar filename
        /// </summary>
        public string ImageSource { get; set; }

        // Overview
        /// <summary>
        /// Average score over all score objects with User ID as Owner ID
        /// </summary>
        public double AverageScore { get; set; }

        // Game
        /// <summary>
        /// Current unique socket ID
        /// </summary>
        public string SocketId { get; set; }

        /// <summary>
        /// Assigned game group
        /// </summary>
        public int GameGroup { get; set; }

        /// <summary>
        /// Is the user ready to play?
        /// </summary>
        public bool ReadyToPlay { get; set; }

        /// <summary>
        /// Did the user write a story?
        /// </summary>
        public bool WroteStory { get; set; }

        /// <summary>
        /// Did the user choose a story?
        /// </summary>
        public bool ChoseStory { get; set; }

        /// <summary>
        /// Is the 'double answer' power-up active?
        /// </summary>
        public bool PowerupOneActive { get; set; }

        /// <summary>
        /// Is the 'double score' power-up active?
        /// </summary>
        public bool PowerupTwoActive { get; set; }

        /// <summary>
        /// Is the 'strike through incorrect answers' power-up active?
        /// </summary>
        public bool PowerupThreeActive { get; set; }

        /// <summary>
        /// Is the 'see amount of votes' power-up active?
        /// </summary>
        public bool PowerupFourActive { get; set; }
    }
}
