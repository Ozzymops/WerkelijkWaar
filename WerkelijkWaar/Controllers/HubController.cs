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
        /// Navigeer naar Both_Game.cshtml
        /// </summary>
        /// <returns>View</returns>
        public IActionResult Both_Game()
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
        /// Navigeer naar TeacherHub_Configuration.cshtml
        /// </summary>
        /// <returns>View</returns>
        public IActionResult TeacherHub_Configuration()
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
        /// Navigeer naar TeacherHub_StudentOverview.cshtml
        /// </summary>
        /// <returns>View</returns>
        public IActionResult TeacherHub_StudentOverview()
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                HubModel hm = new HubModel();
                hm.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                // Haal data op
                hm.UserList = dq.RetrieveUserListByGroup(hm.User.Group);

                return View(hm);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}