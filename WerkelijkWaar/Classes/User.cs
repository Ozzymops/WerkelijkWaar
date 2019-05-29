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

        // Overview
        public double AverageScore { get; set; }

        // Game
        public string SocketId { get; set; }
        public int GameGroup { get; set; }
        public bool ReadyToPlay { get; set; }
        public bool WroteStory { get; set; }
        public bool ChoseStory { get; set; }
        public bool PowerupOneActive { get; set; }
        public bool PowerupTwoActive { get; set; }
        public bool PowerupThreeActive { get; set; }
        public bool PowerupFourActive { get; set; }
    }
}
