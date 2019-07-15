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
using static System.Net.Mime.MediaTypeNames;

namespace WerkelijkWaar.Controllers
{
    public class AccountController : Controller
    {
        // Standard classes
        Classes.DatabaseQueries dq = new Classes.DatabaseQueries();
        Classes.Logger logger = new Classes.Logger();

        // Hard coded admin account
        private string adminUsername = "ADM674382907";
        private string adminPassword = "PASS37219874";

        /// <summary>
        /// Returns information about the hosting environment - used to get the server path for uploading avatars
        /// </summary>
        private readonly IHostingEnvironment hostingEnvironment;

        /// <summary>
        /// CONSTRUCTOR: retrieve the hosting environment and save this to hostingEnvironment
        /// </summary>
        /// <param name="environment">Hosting environment</param>
        public AccountController(IHostingEnvironment environment)
        {
            hostingEnvironment = environment;
        }

        /// <summary>
        /// Redirect user to the correct hub
        /// </summary>
        /// <returns>View</returns>
        public IActionResult Login()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                try
                {
                    Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                    logger.Log("[AccountController - Login]", "(" + tempUser.Id + ") " + tempUser.Username + " validated. Navigating to...", 0, 2, false);

                    // Student hub
                    if (tempUser.RoleId == 0)
                    {
                        logger.Log("[AccountController - Login]", "Student hub.", 2, 2, false);
                        return RedirectToAction("Game", "Hub");
                    }
                    // Teacher hub
                    else if (tempUser.RoleId == 1)
                    {
                        logger.Log("[AccountController - Login]", "Teacher hub.", 2, 2, false);
                        return RedirectToAction("Game", "Hub");
                    }
                    // Admin hub
                    else if (tempUser.RoleId == 2 || tempUser.RoleId == 3)
                    {
                        logger.Log("[AccountController - Login]", "Admin hub.", 2, 2, false);
                        return RedirectToAction("Log", "Hub");
                    }
                }
                catch (Exception exception)
                {
                    logger.Log("[AccountController - Login]", "Something went wrong:\n" + exception, 2, 2, false);
                    return RedirectToAction("Index", "Home");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Save session to cookies and redirect via Login()
        /// </summary>
        /// <param name="loginModel">LoginModel</param>
        /// <returns>View</returns>
        public IActionResult LoginUser(LoginModel loginModel)
        {
            try
            {
                if (String.IsNullOrEmpty(loginModel.Username) && String.IsNullOrEmpty(loginModel.Password))
                {
                    loginModel.Username = null;
                    loginModel.Password = null;
                    loginModel.Status = "Fout: voer je gebruikersnaam en/of wachtwoord in.";

                    return RedirectToAction("Index", "Home", loginModel);
                }

                int userId = dq.CheckLogin(loginModel.Username, loginModel.Password);

                if (userId != 0)
                {
                    Classes.User tempUser = dq.RetrieveUser(userId);
                    HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(tempUser));

                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    // hard coded login
                    if (loginModel.Username == adminUsername && loginModel.Password == adminPassword)
                    {
                        Classes.User tempUser = new Classes.User { Id = 0, Name = "Superadmin", Username = "Superadmin",
                            Surname = "Superadmin", Password = adminPassword, ImageSource = null, RoleId = 3, SpecialFlag = true };

                        HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(tempUser));

                        logger.Log("[AccountController - Login]", "Hardcoded superadmin logged in.", 2, 2, false);

                        return RedirectToAction("Login", "Account");
                    }

                    loginModel.Username = null;
                    loginModel.Password = null;
                    loginModel.Status = "Fout: voer je gebruikersnaam en/of wachtwoord in.";

                    return RedirectToAction("Index", "Home", loginModel);
                }
            }
            catch (Exception exception)
            {
                logger.Log("[AccountController - Login]", "Something went wrong:\n" + exception, 2, 2, false);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="registerModel">RegisterModel</param>
        /// <returns>View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterUser(RegisterModel registerModel)
        {
            try
            {
                if (String.IsNullOrEmpty(registerModel.Username) && String.IsNullOrEmpty(registerModel.Password) &&
                    String.IsNullOrEmpty(registerModel.Name) && String.IsNullOrEmpty(registerModel.Surname) &&
                    registerModel.Group == 0)
                {
                    registerModel.Password = null;
                    registerModel.StatusString = "Fout: voer alle velden in.";
                }

                if (!registerModel.PrivacyCheck)
                {
                    registerModel.Password = null;
                    registerModel.StatusString = "Fout: je moet akkoord gaan met de privacy policy.";
                }

                // Check if already exists
                if (dq.CheckLogin(registerModel.Username, registerModel.Password) != 0)
                {
                    logger.Log("[AccountController - RegisterUser]", "User already exists.", 0, 2, false);

                    LoginModel loginModel = new LoginModel();

                    loginModel.Username = registerModel.Username;
                    loginModel.Password = registerModel.Password;
                    loginModel.Status = "De gegeven combinatie bestaat al. Log a.u.b. in.";

                    return RedirectToAction("Index", "Home", loginModel);
                }

                // Check content
                if (registerModel != null)
                {
                    logger.Log("[AccountController - RegisterUser]", "User does not yet exist.", 0, 2, false);

                    // Input validation
                    if (!registerModel.Name.Any(c => char.IsLetter(c)) || !registerModel.Surname.Any(c => char.IsLetter(c)))
                    {
                        registerModel.Password = null;
                        registerModel.StatusString = "Fout: jouw voor- en achternaam mogen geen nummers of symbolen bevatten.";

                        logger.Log("[AccountController - RegisterUser]", "User content is not validated.", 2, 2, false);
                        return RedirectToAction("Register", "Account", registerModel);
                    }

                    if (registerModel.Password.Any(c => char.IsUpper(c)) && registerModel.Password.Any(c => char.IsDigit(c)))
                    {
                        logger.Log("[AccountController - RegisterUser]", "User content is validated.", 2, 2, false);

                        // Register and login
                        bool status = (dq.RegisterUser(new Classes.User
                        {
                            Name = registerModel.Name,
                            Surname = registerModel.Surname,
                            Username = registerModel.Username,
                            Password = registerModel.Password,
                            RoleId = 0,
                            Group = Convert.ToInt32(registerModel.Group)
                        }));

                        if (status)
                        {
                            int userId = dq.CheckLogin(registerModel.Username, registerModel.Password);
                            HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(dq.RetrieveUser(userId)));

                            return RedirectToAction("AccountView", "Account");
                        }
                        else
                        {
                            registerModel.Password = null;
                            registerModel.StatusString = "Er is iets mis gegaan.";

                            logger.Log("[AccountController - RegisterUser]", "Something went wrong.", 2, 2, false);
                            return RedirectToAction("Register", "Account", registerModel);
                        }
                    }
                    else
                    {
                        registerModel.Password = null;
                        registerModel.StatusString = "Fout: jouw wachtwoord moet minimaal 1 hoofdletter en 1 nummer bevatten.";

                        logger.Log("[AccountController - RegisterUser]", "User content is not validated.", 2, 2, false);
                        return RedirectToAction("Register", "Account", registerModel);
                    }                 
                }
            }
            catch (Exception exception)
            {
                registerModel.Password = null;
                registerModel.StatusString = "Er is iets mis gegaan.";

                logger.Log("[AccountController - RegisterUser]", "Something went wrong:\n" + exception, 2, 2, false);
            }

            return RedirectToAction("Register", "Account", registerModel);
        }

        /// <summary>
        /// Delete current session cookies, forcing a logout
        /// </summary>
        /// <returns>View</returns>
        public IActionResult Logout()
        {
            Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));
            logger.Log("[AccountController - Logout]", "User validated.", 0, 2, false);

            HttpContext.Session.Remove("User");

            logger.Log("[AccountController - Logout]", "(" + tempUser.Id + ")" + tempUser.Username + " logged out.", 2, 2, false);

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigate to Register page
        /// </summary>
        /// <returns>View</returns>
        public IActionResult Register(RegisterModel registerModel)
        {
            try
            {
                if (registerModel == null)
                {
                    registerModel = new RegisterModel();
                }
            }
            catch (Exception exception)
            {
                logger.Log("[AccountController - Register]", "Something went wrong:\n" + exception, 2, 2, false);
                return RedirectToAction("Index", "Home");
            }

            return View(registerModel);
        }

        /// <summary>
        /// Navigate to AccountView
        /// </summary>
        /// <returns>View</returns>
        public IActionResult AccountView()
        {
            // check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                try
                {
                    Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));
                    logger.Log("[AccountController - AccountView]", "User validated.", 0, 2, false);

                    AccountViewModel accountViewModel = new AccountViewModel();
                    accountViewModel.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                    logger.Log("[AccountController - AccountView]", "(" + tempUser.Id + ")" + tempUser.Username + " opening AccountView.", 2, 2, false);
                    return View(accountViewModel);
                }
                catch (Exception exception)
                {
                    logger.Log("[AccountController - AccountView]", "Something went wrong:\n" + exception, 2, 2, false);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Update the name, surname and/or username of a user
        /// </summary>
        /// <param name="accountViewModel">AccountViewModel</param>
        /// <returns>View</returns>
        public IActionResult EditName(AccountViewModel accountViewModel)
        {
            // check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                try
                {
                    logger.Log("[AccountController - EditName]", "User validated.", 0, 2, false);

                    if (!String.IsNullOrEmpty(accountViewModel.User.Name) && !String.IsNullOrEmpty(accountViewModel.User.Surname) && !String.IsNullOrEmpty(accountViewModel.User.Username))
                    {
                        if (accountViewModel.User.Name.Any(c => char.IsLetter(c)) || accountViewModel.User.Surname.Any(c => char.IsLetter(c)))
                        {
                            accountViewModel.User.Id = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User")).Id;

                            logger.Log("[AccountController - EditName]", "Parameters are valid. Updating...", 1, 2, false);

                            bool edited = dq.EditUserNames(accountViewModel.User);

                            if (edited)
                            {
                                accountViewModel.StatusLocation = 0;
                                accountViewModel.StatusString = "Succes.";

                                logger.Log("[AccountController - EditName]", "(" + accountViewModel.User.Id + ")'s Name(s) have been successfully updated.", 2, 2, false);

                                Classes.User tempUser = dq.RetrieveUser(Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User")).Id);
                                HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(tempUser));
                            }
                            else
                            {
                                accountViewModel.StatusLocation = 0;
                                accountViewModel.StatusString = "Er is iets mis gegaan. Probeer het opnieuw.";

                                logger.Log("[AccountController - EditName]", "Something went wrong.", 2, 2, false);
                            }
                        }
                        else
                        {
                            accountViewModel.StatusLocation = 0;
                            accountViewModel.StatusString = "Fout: jouw voor- en achternaam mogen geen nummers of symbolen bevatten.";

                            logger.Log("[AccountController - EditName]", "User content is not validated.", 2, 2, false);
                            return View("AccountView", accountViewModel);
                        }
                    }
                    else
                    {
                        accountViewModel.StatusLocation = 0;
                        accountViewModel.StatusString = "Fout: vul jouw naam, achternaam of gebruikersnaam correct in.";

                        logger.Log("[AccountController - EditName]", "User content is not validated.", 2, 2, false);
                        return View("AccountView", accountViewModel);
                    }

                    return View("AccountView", accountViewModel);
                }
                catch (Exception exception)
                {
                    logger.Log("[AccountController - EditName]", "Something went wrong:\n" + exception, 2, 2, false);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Update the password of a user
        /// </summary>
        /// <param name="accountViewModel">AccountViewModel</param>
        /// <returns>View</returns>
        public IActionResult EditPassword(AccountViewModel accountViewModel)
        {
            // check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                try
                {
                    logger.Log("[AccountController - EditPassword]", "User validated.", 0, 2, false);

                    accountViewModel.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                    if (!String.IsNullOrEmpty(accountViewModel.OldPassword) && !String.IsNullOrEmpty(accountViewModel.NewPassword) && !String.IsNullOrEmpty(accountViewModel.NewPasswordConfirmation))
                    {
                        if (accountViewModel.NewPassword.Any(c => char.IsUpper(c)) && accountViewModel.NewPassword.Any(c => char.IsDigit(c)))
                        {
                            if (accountViewModel.NewPassword == accountViewModel.NewPasswordConfirmation)
                            {
                                logger.Log("[AccountController - EditPassword]", "Parameters are valid. Updating...", 1, 2, false);

                                int userId = dq.CheckLogin(accountViewModel.User.Username, accountViewModel.OldPassword);

                                if (userId != 0)
                                {
                                    accountViewModel.User.Id = userId;
                                    accountViewModel.User.Password = accountViewModel.NewPassword;

                                    bool edited = dq.EditPassword(accountViewModel.User);

                                    if (edited)
                                    {
                                        accountViewModel.StatusLocation = 1;
                                        accountViewModel.StatusString = "Succes.";

                                        logger.Log("[AccountController - EditPassword]", "(" + accountViewModel.User.Id + ")'s Password has been successfully updated.", 2, 2, false);

                                        Classes.User tempUser = dq.RetrieveUser(Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User")).Id);
                                        HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(tempUser));
                                    }
                                    else
                                    {
                                        accountViewModel.StatusLocation = 1;
                                        accountViewModel.StatusString = "Er is iets mis gegaan. Probeer het opnieuw.";

                                        logger.Log("[AccountController - EditPassword]", "Something went wrong.", 2, 2, false);
                                    }

                                    return View("AccountView", accountViewModel);
                                }
                            }
                            else
                            {
                                accountViewModel.StatusLocation = 1;
                                accountViewModel.StatusString = "Vul a.u.b. de velden correct in.";

                                logger.Log("[AccountController - EditPassword]", "Parameters aren't valid. Aborting...", 2, 2, false);
                            }
                        }
                        else
                        {
                            accountViewModel.StatusLocation = 1;
                            accountViewModel.StatusString = "Vul a.u.b. de velden correct in.";

                            logger.Log("[AccountController - EditPassword]", "Parameters aren't valid. Aborting...", 2, 2, false);
                        }
                    }
                    else
                    {
                        accountViewModel.StatusLocation = 1;
                        accountViewModel.StatusString = "Vul a.u.b. de velden correct in.";

                        logger.Log("[AccountController - EditPassword]", "Parameters aren't valid. Aborting...", 2, 2, false);
                    }

                    return View("AccountView", accountViewModel);                    
                }
                catch (Exception exception)
                {
                    accountViewModel.StatusLocation = 1;
                    accountViewModel.StatusString = "Vul a.u.b. de velden correct in.";

                    logger.Log("[AccountController - EditPassword]", "Something went wrong:\n" + exception, 2, 2, false);
                }
            }

            return View("AccountView", accountViewModel);
        }

        /// <summary>
        /// Update the avatar of a user
        /// </summary>
        /// <param name="accountViewModel">AccountViewModel</param>
        /// <returns>View</returns>
        public IActionResult EditAvatar(AccountViewModel accountViewModel)
        {
            // check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                try
                {
                    logger.Log("[AccountController - EditAvatar]", "User validated.", 0, 2, false);

                    accountViewModel.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                    if (accountViewModel.UploadedAvatar != null)
                    {
                        string fileType = Path.GetFileName(accountViewModel.UploadedAvatar.FileName);
                        string fileName = "";

                        // check filetype
                        if (fileType.EndsWith(".jpg"))
                        {
                            fileName = "avatar_" + accountViewModel.User.Id.ToString() + ".jpg";
                        }
                        else if (fileType.EndsWith(".png"))
                        {
                            fileName = "avatar_" + accountViewModel.User.Id.ToString() + ".png";
                        }
                        else
                        {
                            logger.Log("[AccountController - EditAvatar]", "Parameters aren't valid. Aborting...", 2, 2, false);

                            accountViewModel.StatusString = "Upload a.u.b. een .jpg of .png bestand.";
                            accountViewModel.StatusLocation = 2;

                            return View("AccountView", accountViewModel);
                        }

                        logger.Log("[AccountController - EditAvatar]", "Parameters are valid. Updating...", 1, 2, false);

                        // pathing
                        string loc = Path.Combine(hostingEnvironment.WebRootPath, "content\\image\\avatars\\" + accountViewModel.User.Id);
                        string path = Path.Combine(loc, fileName);

                        if (!Directory.Exists(loc))
                        {
                            Directory.CreateDirectory(loc);

                            logger.Log("[AccountController - EditAvatar]", "Directory does not exist.", 1, 2, false);
                        }
                        else
                        {
                            Directory.Delete(loc, true);
                            Directory.CreateDirectory(loc);

                            logger.Log("[AccountController - EditAvatar]", "Directory already exists.", 1, 2, false);
                        }

                        // move image
                        accountViewModel.UploadedAvatar.CopyTo(new FileStream(path, FileMode.Create));     

                        // Sla het pad op in de database bij de nieuwe gebruiker.
                        bool result = dq.EditUserAvatar(accountViewModel.User.Id, fileName);

                        if (result)
                        {
                            logger.Log("[AccountController - EditAvatar]", "(" + accountViewModel.User.Id + ")'s Avatar has been successfully updated.", 2, 2, false);

                            accountViewModel.StatusString = "Succes.";
                            accountViewModel.StatusLocation = 2;

                            Classes.User tempUser = dq.RetrieveUser(Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User")).Id);
                            HttpContext.Session.SetString("User", Newtonsoft.Json.JsonConvert.SerializeObject(tempUser));
                        }
                        else
                        {
                            logger.Log("[AccountController - EditAvatar]", "Something went wrong.", 2, 2, false);

                            accountViewModel.StatusString = "Er is iets mis gegaan. Probeer het a.u.b. opnieuw.";
                            accountViewModel.StatusLocation = 2;
                        }

                        return View("AccountView", accountViewModel);
                    }
                    else
                    {
                        accountViewModel.StatusLocation = 2;
                        accountViewModel.StatusString = "Upload a.u.b. een avatar.";

                        logger.Log("[AccountController - EditAvatar]", "Parameters aren't valid. Aborting...", 2, 2, false);

                        return View("AccountView", accountViewModel);
                    }         
                }
                catch (Exception exception)
                {
                    logger.Log("[AccountController - EditAvatar]", "Something went wrong:\n" + exception, 2, 2, false);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Update a user (as admin)
        /// </summary>
        /// <param name="adminModel">AdminModel</param>
        /// <returns></returns>
        public IActionResult EditUser(AdminModel adminModel)
        {
            // check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                try
                {
                    logger.Log("[AccountController - EditUser]", "User validated.", 0, 2, false);

                    if (!String.IsNullOrEmpty(adminModel.User.Name) && !String.IsNullOrEmpty(adminModel.User.Surname) && !String.IsNullOrEmpty(adminModel.User.Username))
                    {
                        if (adminModel.User.Name.Any(c => char.IsLetter(c)) || adminModel.User.Surname.Any(c => char.IsLetter(c)))
                        {
                            logger.Log("[AccountController - EditUser]", "Parameters are valid. Updating...", 1, 2, false);

                            bool editedNames = dq.EditUserNames(adminModel.User);

                            if (editedNames)
                            {
                                logger.Log("[AccountController - EditUser]", "(" + adminModel.User.Id + ")'s Name(s) have been successfully updated.", 1, 2, false);

                                bool editedNumbers = true;

                                if (editedNumbers)
                                {
                                    logger.Log("[AccountController - EditUser]", "(" + adminModel.User.Id + ")'s Role ID and/or Group ID has been successfully updated.", 1, 2, false);

                                    if (adminModel.UploadedAvatar != null)
                                    {
                                        string fileType = Path.GetFileName(adminModel.UploadedAvatar.FileName);
                                        string fileName = "";

                                        // check filetype
                                        if (fileType.EndsWith(".jpg"))
                                        {
                                            fileName = "avatar_" + adminModel.User.Id.ToString() + ".jpg";
                                        }
                                        else if (fileType.EndsWith(".png"))
                                        {
                                            fileName = "avatar_" + adminModel.User.Id.ToString() + ".png";
                                        }
                                        else
                                        {
                                            adminModel.StatusString = "Upload a.u.b. een .jpg of .png bestand.";

                                            return RedirectToAction("IndividualUsers", "Overview", adminModel);
                                        }

                                        // pathing
                                        string loc = Path.Combine(hostingEnvironment.WebRootPath, "content\\image\\avatars\\" + adminModel.User.Id);
                                        string path = Path.Combine(loc, fileName);
                                        Directory.CreateDirectory(loc);

                                        // move image
                                        adminModel.UploadedAvatar.CopyTo(new FileStream(path, FileMode.Create));

                                        bool editedAvatar = dq.EditUserAvatar(adminModel.User.Id, fileName);

                                        if (editedAvatar)
                                        {
                                            logger.Log("[AccountController - EditUser]", "(" + adminModel.User.Id + ")'s Avatar has been successfully updated.", 2, 2, false);

                                            adminModel.StatusString = "Succes.";
                                        }
                                        else
                                        {
                                            logger.Log("[AccountController - EditUser]", "Something went wrong.", 2, 2, false);

                                            adminModel.StatusString = "Er is iets mis gegaan. Probeer het opnieuw.";
                                        }
                                    }

                                    adminModel.StatusString = "Succes.";
                                }
                                else
                                {
                                    logger.Log("[AccountController - EditUser]", "Something went wrong.", 2, 2, false);

                                    adminModel.StatusString = "Er is iets mis gegaan. Probeer het opnieuw.";
                                }
                            }
                            else
                            {
                                logger.Log("[AccountController - EditUser]", "Parameters aren't valid. Aborting...", 2, 2, false);

                                adminModel.StatusString = "Vul a.u.b. de velden correct in.";
                            }
                        }
                        else
                        {
                            logger.Log("[AccountController - EditUser]", "Parameters aren't valid. Aborting...", 2, 2, false);

                            adminModel.StatusString = "Vul a.u.b. de velden correct in.";
                        }
                    }

                    return RedirectToAction("IndividualUsers", "Overview", new { userId = adminModel.User.Id });
                }
                catch (Exception exception)
                {
                    logger.Log("[AccountController - EditUser]", "Something went wrong:\n" + exception, 2, 2, false);
                }                
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Delete a account belonging to a certain User ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="selfDelete">Is this a admin action or a self-inflicted delete?</param>
        /// <returns>View</returns>
        public IActionResult DeleteAccount(int userId, bool selfDelete)
        {
            // check if logged in as the user it's trying to destroy
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                try
                {
                    logger.Log("[AccountController - DeleteAccount]", "User validated.", 0, 2, false);

                    Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                    if (tempUser.Id == userId || tempUser.RoleId == 2 || tempUser.RoleId == 3)
                    {
                        logger.Log("[AccountController - DeleteAccount]", "Deleting (" + userId + ")...", 1, 2, false);

                        bool deletedStories = dq.DeleteStoriesOfUser(userId);
                        bool deletedScores = dq.DeleteScoresOfUser(userId);
                        bool deletedConfig = dq.DeleteConfig(userId);

                        bool deleted = dq.DeleteUser(userId);

                        if (deleted)
                        {
                            logger.Log("[AccountController - DeleteAccount]", "Successfully deleted (" + userId + ").", 2, 2, false);

                            if (selfDelete)
                            {
                                HttpContext.Session.Remove("User");
                            }
                            else
                            {
                                return RedirectToAction("UserOverview", "Overview");
                            }
                        }
                        else
                        {
                            logger.Log("[AccountController - DeleteAccount]", "Something went wrong.", 2, 2, false);
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.Log("[AccountController - DeleteAccount]", "Something went wrong:\n" + exception, 2, 2, false);
                }                
            }

            return RedirectToAction("Index", "Home");
        }
    }
}