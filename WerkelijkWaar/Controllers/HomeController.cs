using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WerkelijkWaar.Models;

namespace WerkelijkWaar.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Navigate to Index
        /// </summary>
        /// <param name="loginModel"></param>
        /// <returns>View</returns>
        public IActionResult Index(LoginModel loginModel)
        {
            if (loginModel == null)
            {
                loginModel = new LoginModel();
            }

            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Account");
            }

            return View(loginModel);
        }

        /// <summary>
        /// Navigate to Register
        /// </summary>
        /// <returns>View</returns>
        public IActionResult GoToRegister()
        {
            return RedirectToAction("Register", "Account");
        }

        /// <summary>
        /// Navigate to GameInfo
        /// </summary>
        /// <returns>View</returns>
        public IActionResult GameInfo()
        {
            return View();
        }

        /// <summary>
        /// Navigate to Privacy
        /// </summary>
        /// <returns>View</returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Return ErrorViewModel
        /// </summary>
        /// <returns>View</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
