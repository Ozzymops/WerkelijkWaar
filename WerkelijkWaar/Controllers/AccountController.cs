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

        /// <summary>
        /// Open de accountoverzicht met gebruikersdata.
        /// </summary>
        /// <returns>View</returns>
        public IActionResult AccountView()
        {
            // check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                AccountViewModel avm = new AccountViewModel();
                avm.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                return View(avm);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Open het accountgegevens-veranderen-scherm.
        /// </summary>
        /// <returns>View</returns>
        public IActionResult EditAccount()
        {
            // check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                EditAccountModel eam = new EditAccountModel();
                eam.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                return View(eam);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Wijzig de gegevens van een gebruiker
        /// </summary>
        /// <param name="eam">EditAccountModel</param>
        /// <returns>View</returns>
        public IActionResult EditGenericData(EditAccountModel eam)
        {
            // check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                Classes.User newUser = new Classes.User { Id = eam.Id };

                // Check data
                if (String.IsNullOrEmpty(eam.NewName) || String.IsNullOrEmpty(eam.NewSurname) || String.IsNullOrEmpty(eam.NewUsername))
                {
                    eam.StatusString = "Vul a.u.b. de velden correct in.";
                    return RedirectToAction("EditAccount", "Account", eam);
                }

                newUser.Name = eam.NewName;
                newUser.Surname = eam.NewSurname;
                newUser.Username = eam.NewUsername;

                bool edited = dq.EditUser(newUser);

                if (edited)
                {
                    eam.StatusString = "Succes!";
                    HttpContext.Session.Remove("User");
                    HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(dq.RetrieveUser(eam.Id)));
                }
                else
                {
                    eam.StatusString = "Er is iets fout gegaan of je hebt niks ingevuld. Check je gegevens.";
                }

                return RedirectToAction("EditAccount", "Account", eam);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult EditPassword()
        {
            // match password with checklogin!
            return RedirectToAction("EditAccount", "Account");
        }

        public IActionResult EditAvatar()
        {
            return RedirectToAction("EditAccount", "Account");
        }

        public IActionResult DeleteAccount(int id)
        {
            // check if logged in as the user it's trying to destroy
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.Id == id)
                {
                    bool deleted = dq.DeleteUser(id);

                    if (deleted)
                    {
                        HttpContext.Session.Remove("User");
                    }
                }
            }

            return RedirectToAction("Index", "Home");
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