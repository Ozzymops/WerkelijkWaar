using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WerkelijkWaar.Models;

namespace WerkelijkWaar.Controllers
{
    public class AccountController : Controller
    {
        // Standaard, overal toepasselijk
        Classes.Logger l = new Classes.Logger();
        Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

        /// <summary>
        /// Navigeer naar Login.cshtml.
        /// </summary>
        /// <param name="screen">Schermtype.</param>
        /// <param name="destination">Bestemming.</param>
        /// <returns>View</returns>
        public IActionResult Login(int screen, int destination)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                if (screen == 0)
                {
                    if (destination == 0)
                    {
                        // Configuration
                        return RedirectToAction("Privacy", "Home");
                    }
                    else if (destination == 1)
                    {
                        // Inzage
                        return RedirectToAction("Privacy", "Home");
                    }
                }
                else if (screen == 1)
                {
                    // Game
                    return RedirectToAction("Index", "Home");
                }
            }

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
            int id = dq.CheckLogin(lm.Username, lm.Password);

            if (id != 0)
            {
                HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(dq.RetrieveUser(id)));

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
                        return RedirectToAction("Privacy", "Home");
                    }
                }
                else if (lm.Screen == 1)
                {
                    // Game
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Registreer een gebruiker.
        /// </summary>
        /// <param name="rm">RegisterModel</param>
        /// <returns>View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterUser(RegisterModel rm)
        {
            l.WriteToLog("[RegisterUser]", "Executing RegisterUser.", 0);

            // Check if already exists
            if (dq.CheckLogin(rm.Username, rm.Password) != 0)
            {
                l.WriteToLog("[RegisterUser]", "Attempted combination is already in use.", 1);
                return RedirectToAction("Index", "Home");
            }

            // Check inhoud
            if (rm != null)
            {
                l.WriteToLog("[RegisterUser]", "rm != null", 2);

                bool valid = false;
                var reg = new Regex("[^a-zA-Z0-9_.]");

                // Input validatie
                if (!reg.IsMatch(rm.Name))
                {
                    l.WriteToLog("[RegisterUser]", "Name matched.", 2);
                    if (!reg.IsMatch(rm.Surname))
                    {
                        l.WriteToLog("[RegisterUser]", "Surname matched.", 2);
                        if (rm.RoleId.GetType() == typeof(int))
                        {
                            l.WriteToLog("[RegisterUser]", "RoleId matched.", 2);
                            if (rm.AgeCheck)
                            {
                                l.WriteToLog("[RegisterUser]", "Age matched.", 2);
                                if (rm.PrivacyCheck)
                                {
                                    l.WriteToLog("[RegisterUser]", "Privacy matched.", 2);
                                    // check code
                                    valid = true;
                                }
                            }
                        }
                    }

                    if (valid)
                    {
                        l.WriteToLog("[RegisterUser]", "valid = true", 1);

                        // register and login
                        bool status = (dq.RegisterUser(new Classes.User
                        {
                            Name = rm.Name,
                            Surname = rm.Surname,
                            Username = rm.Surname,
                            Password = rm.Password,
                            RoleId = rm.RoleId
                        }));

                        int id = dq.CheckLogin(rm.Username, rm.Password);
                        HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(dq.RetrieveUser(id)));
                    }
                    else
                    {
                        l.WriteToLog("[RegisterUser]", "Aborting RegisterUser (valid = false).", 1);
                    }
                }           
            }
            else
            {
                l.WriteToLog("[RegisterUser]", "Aborting RegisterUser (rm = null).", 1);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Verwijder de huidige sessie. Dit zorgt voor een logout.
        /// </summary>
        /// <returns>View</returns>
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("User");
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult EditAccount()
        {
            return View();
        }

        public IActionResult RemoveAccount()
        {
            return View();
        }

        public IActionResult DownloadAccount()
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