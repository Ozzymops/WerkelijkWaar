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
        public IActionResult Index()
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        public IActionResult GoToLogin(int scr, int des)
        {
            return RedirectToAction("Login", "Account", new { screen = scr, destination = des });
        }

        public IActionResult GoToRegister()
        {
            return RedirectToAction("Register", "Account");
        }

        public IActionResult GoToConfig()
        {
            return GoToLogin(0, 0);
        }

        public IActionResult GoToOverviews()
        {
            return GoToLogin(0, 1);
        }

        public IActionResult GameInfo()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
