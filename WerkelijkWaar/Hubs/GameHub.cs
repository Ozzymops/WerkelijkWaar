﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace WerkelijkWaar.Hubs
{
    public class GameHub : Hub
    {
        public async Task SendMessage(string userId, string action, string code, string type)
        {
            await Clients.All.SendAsync("ReceiveMessage", userId, action, code, type);
        }
    }
}
