using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WerkelijkWaar.Models;

namespace WerkelijkWaar.Controllers
{
    public class AccountController : Controller
    {
        // Standaard, overal toepasselijk
        Classes.Logger l = new Classes.Logger();
        Stopwatch sw = new Stopwatch();

        Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

        /// <summary>
        /// Geeft informatie over de hosting environment terug - nodig om serverpad te kunnen krijgen.
        /// </summary>
        private readonly IHostingEnvironment hostingEnvironment;

        /// <summary>
        /// Constructor: haal de hosting environment op en sla deze in hostingEnvironment op.
        /// </summary>
        /// <param name="environment">De hosting environment.</param>
        public AccountController(IHostingEnvironment environment)
        {
            hostingEnvironment = environment;
        }

        /// <summary>
        /// Stuur de gebruiker door naar de juiste hub.
        /// </summary>
        /// <returns>View</returns>
        public IActionResult Login()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                l.WriteToLog("[AccountController]", tempUser.Username + " validated.", 0);

                // Student hub
                if (tempUser.RoleId == 0)
                {
                    l.WriteToLog("[AccountController]", tempUser.Username + " navigated to Hub/Game", 2);
                    return RedirectToAction("Game", "Hub");
                }
                // Teacher hub
                else if (tempUser.RoleId == 1)
                {
                    l.WriteToLog("[AccountController]", tempUser.Username + " navigated to Hub/Game", 2);
                    return RedirectToAction("Game", "Hub");
                }
                // Admin hub
                else if (tempUser.RoleId == 2 || tempUser.RoleId == 3)
                {
                    l.WriteToLog("[AccountController]", tempUser.Username + " navigated to Hub/Game", 2);
                    return RedirectToAction("Log", "Hub");
                }
            }

            return RedirectToAction("Index", "Home");
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
                Classes.User tempUser = dq.RetrieveUser(id);
                HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(tempUser));

                return RedirectToAction("Login", "Account");
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
            // Check if already exists
            if (dq.CheckLogin(rm.Username, rm.Password) != 0)
            {
                LoginModel lm = new LoginModel();
                lm.Username = rm.Username;
                lm.Password = rm.Password;
                lm.Status = "De gegeven combinatie bestaat al. Log a.u.b. in.";
                return RedirectToAction("Index", "Home", lm);
            }

            // Check inhoud
            if (rm != null)
            {
                bool valid = false;
                var reg = new Regex("[^a-zA-Z0-9_.]");

                // Input validatie
                if (!reg.IsMatch(rm.Name))
                {
                    if (!reg.IsMatch(rm.Surname))
                    {
                        if (rm.RoleId.GetType() == typeof(int))
                        {
                            if (rm.Group.GetType() == typeof(int))
                            {
                                if (rm.PrivacyCheck)
                                {
                                    valid = true;
                                }
                            }
                        }
                    }

                    if (valid)
                    {
                        // register and login
                        bool status = (dq.RegisterUser(new Classes.User
                        {
                            Name = rm.Name,
                            Surname = rm.Surname,
                            Username = rm.Username,
                            Password = rm.Password,
                            RoleId = 0,
                            Group = Convert.ToInt32(rm.Group)
                        }));

                        if (status)
                        {
                            int id = dq.CheckLogin(rm.Username, rm.Password);
                            HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(dq.RetrieveUser(id)));

                            return RedirectToAction("AccountView", "Account");
                        }                   
                    }
                }           
            }

            return RedirectToAction("Register", "Account");
        }

        /// <summary>
        /// Verwijder de huidige sessie. Dit zorgt voor een logout.
        /// </summary>
        /// <returns>View</returns>
        public IActionResult Logout()
        {
            Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));
            l.WriteToLog("[AccountController]", tempUser.Username + " validated.", 0);

            HttpContext.Session.Remove("User");

            l.WriteToLog("[AccountController]", tempUser.Username + " logged out.", 2);

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
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));
                l.WriteToLog("[AccountController]", tempUser.Username + " validated.", 0);

                AccountViewModel avm = new AccountViewModel();
                avm.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                l.WriteToLog("[AccountController]", tempUser.Username + " navigated to Account/AccountView.", 2);
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

        public IActionResult EditName(AccountViewModel avm)
        {
            // check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                avm.User.Id = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User")).Id;

                if (String.IsNullOrEmpty(avm.User.Name) || String.IsNullOrEmpty(avm.User.Surname) || String.IsNullOrEmpty(avm.User.Username))
                {
                    avm.StatusString = "Vul a.u.b. de velden correct in.";
                    avm.StatusLocation = 0;
                }
                else
                {
                    bool edited = dq.EditUserNames(avm.User);

                    if (edited)
                    {
                        avm.StatusString = "Succes.";
                        avm.StatusLocation = 0;
                    }
                    else
                    {
                        avm.StatusString = "Er is iets mis gegaan. Probeer het opnieuw.";
                        avm.StatusLocation = 0;
                    }
                }

                return View("AccountView", avm);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult EditPassword(AccountViewModel avm)
        {
            // check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                avm.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (String.IsNullOrEmpty(avm.NewPassword) || String.IsNullOrEmpty(avm.NewPasswordConfirmation) || String.IsNullOrEmpty(avm.OldPassword))
                {
                    avm.StatusString = "Vul a.u.b. de velden correct in.";
                    avm.StatusLocation = 1;
                }
                else
                {
                    // check if passwords match
                    if (avm.NewPassword == avm.NewPasswordConfirmation)
                    {
                        int id = dq.CheckLogin(avm.User.Username, avm.OldPassword);
                        if (id != 0)
                        {
                            avm.User.Id = id;
                            avm.User.Password = avm.NewPassword;

                            bool edited = dq.EditPassword(avm.User);

                            if (edited)
                            {
                                avm.StatusString = "Succes.";
                                avm.StatusLocation = 1;
                            }
                            else
                            {
                                avm.StatusString = "Er is iets mis gegaan. Probeer het opnieuw.";
                                avm.StatusLocation = 1;
                            }
                        }
                        else
                        {
                            avm.StatusString = "Vul a.u.b. uw oude wachtwoord correct in.";
                            avm.StatusLocation = 1;
                        }
                    }
                    else
                    {
                        avm.StatusString = "Het nieuwe wachtwoord komt niet overeen met de bevestigingswachtwoord.";
                        avm.StatusLocation = 1;
                    }
                }

                return View("AccountView", avm);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult EditAvatar(EditAccountModel eam)
        {
            // check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Sla de image tijdelijk op.
                var img = eam.NewAvatar;

                // Haal bestandstype op
                var fileType = Path.GetFileName(img.FileName);

                // Haal de filename van de image op.
                var fileName = "";

                if (fileType.EndsWith(".jpg"))
                {
                    fileName = "avatar_" + eam.Id.ToString() + ".jpg";
                }
                else if (fileType.EndsWith(".png"))
                {
                    fileName = "avatar_" + eam.Id.ToString() + ".png";
                }
                else
                {
                    return RedirectToAction("EditAccount", "Account", eam);
                }

                // todo: check of image daadwerkelijk een image is.

                // Haal de User ID op van de nieuwe gebruiker.
                int userId = eam.Id;

                // Bouw het pad naar de image op.
                var loc = Path.Combine(hostingEnvironment.WebRootPath, "content\\image\\avatars\\" + userId);
                var path = Path.Combine(loc, fileName);

                // Creëer het pad naar de image.
                Directory.CreateDirectory(loc);

                // Kopiëer de image naar het pad.
                img.CopyTo(new FileStream(path, FileMode.Create));

                // Sla het pad op in de database bij de nieuwe gebruiker.
                bool result = dq.EditUserAvatar(eam.Id, fileName);

                if (result)
                {
                    HttpContext.Session.Remove("User");
                    HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(dq.RetrieveUser(eam.Id)));
                }
            }

            return RedirectToAction("EditAccount", "Account", eam);
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