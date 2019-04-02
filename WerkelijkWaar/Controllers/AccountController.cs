using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WerkelijkWaar.Models;

namespace WerkelijkWaar.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login(int screen, int destination)
        {
            LoginModel lm = new LoginModel();
            lm.Screen = screen;
            lm.Destination = destination;

            return View(lm);
        }

        /// <summary>
        /// Log een gebruiker in en herleid de gebruiker naar de gewenste pagina.
        /// </summary>
        /// <param name="lm">LoginModel</param>
        /// <returns>View</returns>
        public IActionResult LoginUser(LoginModel lm)
        {
            if (lm.Screen == 0)
            {
                if (lm.Destination == 0)
                {
                    // Configuration
                    return RedirectToAction("Privacy", "Home");
                }
                else if (lm.Destination == 1)
                {
                    // Inzage
                    return RedirectToAction("Index", "Home");
                }
            }
            else if (lm.Screen == 1)
            {
                // Game
                return View("Home");
            }
            else
            {
                return View("Login");
            }

            return View("Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Personalize()
        {
            return View();
        }

        public IActionResult PersonalData()
        {
            return View();
        }
    }
}