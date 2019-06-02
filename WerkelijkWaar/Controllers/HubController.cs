using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WerkelijkWaar.Models;

namespace WerkelijkWaar.Controllers
{
    public class HubController : Controller
    {
        // Standaard, overal toepasselijk
        Classes.Logger l = new Classes.Logger();
        Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

        /// <summary>
        /// Navigeer naar het spel
        /// </summary>
        /// <returns>View</returns>
        public IActionResult Game()
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                HubModel hm = new HubModel();
                hm.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                return View(hm);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigeer naar de spel configuratiescherm
        /// </summary>
        /// <returns>View</returns>
        public IActionResult GameConfig()
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                HubModel hm = new HubModel();
                hm.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                return View(hm);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Log()
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                AdminModel am = new AdminModel();
                am.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (am.User.RoleId == 2)
                {
                    am.SchoolList = dq.RetrieveSchools();
                    am.GenerateSchoolListItems();
                }
                else if (am.User.RoleId == 3)
                {
                    am.SchoolList = dq.RetrieveSchools();
                    am.GenerateSchoolListItems();
                }

                return View(am);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult ServerConfig()
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                HubModel hm = new HubModel();
                hm.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (hm.User.RoleId == 3)
                {
                    return View(hm);
                }
            }

            return RedirectToAction("Index", "Home");
        }
    }
}