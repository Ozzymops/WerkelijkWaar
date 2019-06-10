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
        // Standard classes
        Classes.DatabaseQueries dq = new Classes.DatabaseQueries();
        Classes.Logger logger = new Classes.Logger();

        /// <summary>
        /// Write a configuration to the database
        /// </summary>
        /// <param name="configModel">ConfigModel</param>
        /// <returns>View</returns>
        public IActionResult CreateConfig(ConfigModel configModel)
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                try
                {
                    logger.Log("[ConfigController - CreateConfig]", "User validated.", 0, 2, false);

                    configModel.Teacher = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                    bool success = false;
                    string response = "";

                    if (dq.RetrieveConfig(configModel.Teacher.Id) == null || configModel.Config.Id == 0)
                    {
                        logger.Log("[ConfigController - CreateConfig]", "Configuration of (" + configModel.Teacher.Id + ") does not exist yet. Creating...", 1, 2, false);

                        success = dq.CreateConfig(configModel.Config);
                    }
                    else
                    {
                        logger.Log("[ConfigController - CreateConfig]", "Configuration of (" + configModel.Teacher.Id + ") already exists. Updating...", 1, 2, false);

                        success = dq.UpdateConfig(configModel.Config);
                    }

                    if (success)
                    {
                        logger.Log("[ConfigController - CreateConfig]", "Created/updated (" + configModel.Teacher.Id + ")'s configuration.", 2, 2, false);

                        response = "Succes.";
                    }
                    else
                    {
                        logger.Log("[ConfigController - CreateConfig]", "Something went wrong.", 2, 2, false);

                        response = "Er is iets mis gegaan.";
                    }

                    return RedirectToAction("GameConfig", "Hub", response);
                }
                catch (Exception exception)
                {
                    logger.Log("[ConfigController - CreateConfig]", "Something went wrong:\n" + exception, 2, 2, false);
                }              
            }

            return RedirectToAction("Index", "Home");
        }
    }
}