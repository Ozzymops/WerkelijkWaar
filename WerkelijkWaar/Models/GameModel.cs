﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class GameModel
    {
        public int CurrentUserId { get; set; }
        public int CurrentUserRole { get; set; }
        public string GameCode { get; set; }
        public bool LobbyHosted { get; set; }
        public Classes.Lobby Lobby { get; set; }
    }
}
