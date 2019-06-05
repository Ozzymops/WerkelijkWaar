using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WerkelijkWaar.Models;

namespace WerkelijkWaar.Controllers
{
    public class HubController : Controller
    {
        // Standard classes
        Classes.Logger logger = new Classes.Logger();
        Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

        /// <summary>
        /// Navigeer to Game
        /// </summary>
        /// <returns>View</returns>
        public IActionResult Game()
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                HubModel hubModel = new HubModel();
                hubModel.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                return View(hubModel);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigate to GameConfig
        /// </summary>
        /// <param name="StatusString">Status string</param>
        /// <returns></returns>
        public IActionResult GameConfig(string StatusString)
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                ConfigModel configModel = new ConfigModel();
                configModel.StatusString = StatusString;
                configModel.Teacher = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));
                configModel.Config = dq.RetrieveConfig(configModel.Teacher.Id);

                return View(configModel);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigate to Log
        /// </summary>
        /// <returns></returns>
        public IActionResult Log()
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                AdminModel adminModel = new AdminModel();
                adminModel.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (adminModel.User.RoleId == 2)
                {
                    adminModel.Group = dq.RetrieveGroup(adminModel.User.Group);
                    adminModel.School = dq.RetrieveSchool(adminModel.Group.SchoolId);
                }
                else if (adminModel.User.RoleId == 3)
                {
                    List<Classes.School> tempSchoolList = dq.RetrieveSchools();
                    List<Classes.School> schoolList = new List<Classes.School>();

                    foreach (Classes.School school in tempSchoolList)
                    {
                        schoolList.Add(new Classes.School { Id = school.Id, SchoolName = school.SchoolName });
                    }

                    adminModel.SchoolList = new SelectList(schoolList, "Id", "SchoolName");
                }

                return View(adminModel);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigate to ServerConfig
        /// </summary>
        /// <returns></returns>
        public IActionResult ServerConfig()
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                HubModel hubModel = new HubModel();
                hubModel.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (hubModel.User.RoleId == 3)
                {
                    return View(hubModel);
                }
            }

            return RedirectToAction("Index", "Home");
        }
    }
}