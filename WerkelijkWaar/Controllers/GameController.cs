using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WerkelijkWaar.Controllers
{
    public class GameController : Controller
    {
        // Standaard, overal toepasselijk
        Classes.Logger l = new Classes.Logger();
        Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

        public IActionResult OpenLobbyView()
        {
            return PartialView("_GameLobby");
        }

        public IActionResult CreateLobby()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                // Check of gebruiker een docent is
                if (tempUser.RoleId == 1)
                {
                    l.WriteToLog("[CreateLobby]", "Building lobby.", 0);

                    // Geef GameCode terug en open een PartialView
                    Models.GameModel gm = new Models.GameModel();

                    gm.CurrentUserId = tempUser.Id;
                    gm.CurrentUserRole = tempUser.RoleId;
                    gm.Lobby = new Classes.Lobby();
                    gm.Lobby.GenerateCode();
                    // Haal configuratie op
                    gm.GameCode = gm.Lobby.Code;

                    l.WriteToLog("[CreateLobby]", "Attempting to return partial view. Code is " + gm.Lobby.Code, 1);
                    return PartialView("_GameLobby", gm);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult JoinLobby(string code)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                // Check of gebruiker een student is
                if (tempUser.RoleId == 0)
                {
                    l.WriteToLog("[JoinLobby]", "Building lobby.", 0);

                    // Geef GameCode terug en open een PartialView
                    Models.GameModel gm = new Models.GameModel();

                    gm.CurrentUserId = tempUser.Id;
                    gm.CurrentUserRole = tempUser.RoleId;
                    gm.Lobby = new Classes.Lobby();
                    gm.Lobby.Code = code;
                    // Haal configuratie op
                    gm.GameCode = code;

                    l.WriteToLog("[JoinLobby]", "Attempting to return partial view. Code is " + gm.Lobby.Code, 1);
                    return PartialView("_GameLobby", gm);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult CloseLobby(string gameCode)
        {
            return View();
        }
    }
}