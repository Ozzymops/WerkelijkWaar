using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WerkelijkWaar.Models;

namespace WerkelijkWaar.Controllers
{
    public class ConfigController : Controller
    {
        // Standaard, overal toepasselijk
        Classes.Logger l = new Classes.Logger();
        Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

        public IActionResult CreateConfig(ConfigModel cm)
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                cm.Teacher = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                bool success = false;
                string response = "";

                if (dq.RetrieveConfig(cm.Teacher.Id) == null || cm.Config.Id == 0)
                {
                    l.DebugToLog("[x]", "Config does not exist - " + cm.Config.Id + "/" + cm.Teacher.Id + ".", 1);
                    success = dq.CreateConfig(cm.Config);
                }
                else
                {
                    l.DebugToLog("[x]", "Config does exist - " + cm.Config.Id + "/" + cm.Teacher.Id + ".", 1);
                    success = dq.UpdateConfig(cm.Config);
                }

                if (success)
                {
                    response = "Succes.";
                }
                else
                {
                    response = "Er is iets mis gegaan.";
                }

                return RedirectToAction("GameConfig", "Hub", response);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}