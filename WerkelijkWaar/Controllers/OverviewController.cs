﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WerkelijkWaar.Models;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WerkelijkWaar.Controllers
{
    public class OverviewController : Controller
    {
        // Standaard, overal toepasselijk
        Classes.Logger l = new Classes.Logger();
        Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Navigeer naar de scoreoverzicht.
        /// </summary>
        /// <returns>View</returns>
        public IActionResult ScoreOverview()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                ScoreOverviewModel som = new ScoreOverviewModel();
                som.CurrentUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                // Student
                if (som.CurrentUser.RoleId == 0)
                {
                    // redirect to personal scores
                    return RedirectToAction("IndividualScores", "Overview", new { id = som.CurrentUser.Id, rank = -1, scoreId = 0 });
                }
                // Teacher
                else if (som.CurrentUser.RoleId == 1)
                {
                    // show full overview / table
                    som.UserList = dq.RetrieveUserListByGroup(1);
                    som.GenerateAverage();

                    return View(som);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigeer naar de gebruikersoverzicht.
        /// </summary>
        /// <returns>View</returns>
        public IActionResult UserOverview()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                AdminModel am = new AdminModel();
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2)
                {
                    am.UserList = dq.RetrieveAllUsers();
                    return View(am);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigeer naar de verhalenoverzicht.
        /// </summary>
        /// <returns>View</returns>
        public IActionResult StoryOverview()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                AdminModel am = new AdminModel();
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2)
                {
                    am.StoryList = dq.RetrieveStoryQueue();
                    return View(am);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Zie de geselecteerde score in.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="rank">Ranknummer</param>
        /// <param name="scoreId">Score ID</param>
        /// <returns>View</returns>
        public IActionResult IndividualScores(int id, int rank, int scoreId)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                IndividualModel ism = new IndividualModel();
                ism.GetUser(id);
                ism.GenerateAverage();
                ism.Scores = dq.RetrieveScoresOfUser(ism.User.Id);
                ism.Stories = dq.RetrieveStoriesOfUser(ism.User.Id);
                ism.Rank = rank;
                ism.Role = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User")).RoleId;

                if (ism.Scores != null && ism.Scores.Count != 0)
                {
                    if (scoreId != 0)
                    {
                        foreach (Classes.Score s in ism.Scores)
                        {
                            if (s.Id == scoreId)
                            {
                                ism.Score = s;
                            }
                        }
                    }
                    else
                    {
                        ism.Score = ism.Scores[0];
                    }
                }

                return View(ism);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Zie het geselecteerde verhaal in.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="rank">Ranknummer</param>
        /// <param name="storyId">Story ID</param>
        /// <returns></returns>
        public IActionResult IndividualStories(int id, int rank, int storyId)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                IndividualModel ism = new IndividualModel();
                ism.GetUser(id);
                ism.Stories = dq.RetrieveStoriesOfUser(ism.User.Id);
                ism.Rank = rank;
                ism.Role = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User")).RoleId;

                if (storyId != 0)
                {
                    foreach (Classes.Story s in ism.Stories)
                    {
                        if (s.Id == storyId)
                        {
                            ism.Story = s;
                        }
                    }

                }

                return View(ism);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigeer naar de CreateScore pagina
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>View</returns>
        public IActionResult CreateScore(int id, int rank)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if teacher
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 1)
                {
                    IndividualModel ism = new IndividualModel();
                    ism.GetUser(id);
                    ism.Rank = rank;
                    ism.Score = new Classes.Score();

                    List<Classes.GameType> gameTypeList = new List<Classes.GameType>();
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 2, TypeName = "Spel 1" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 3, TypeName = "Spel 2" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 4, TypeName = "Spel 3" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 5, TypeName = "Spel 4" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 6, TypeName = "Spel 5" });

                    ism.GameType = new SelectList(gameTypeList, "TypeIndex", "TypeName");

                    return View(ism);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Maak een score aan.
        /// </summary>
        /// <param name="ism">IndividualModel</param>
        /// <returns></returns>
        public IActionResult PushScore(IndividualModel im)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if teacher
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 1)
                {
                    im.Score.GameType = im.GameTypeIndex;
                    bool results = dq.CreateScore(im.Score);
                }

                return RedirectToAction("IndividualScores", "Overview", new { id = im.User.Id, rank = im.Rank, scoreId = 0 });
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Zie het geselecteerde verhaal in.
        /// </summary>
        /// <param name="id">Story ID</param>
        /// <returns></returns>
        public IActionResult IndividualStoryConfirmation(int id)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                AdminModel am = new AdminModel();
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2)
                {
                    am.Story = dq.RetrieveStory(id);

                    if (am.Story.Status != 2)
                    {
                        dq.UpdateStoryStatus(id, 1);
                    }

                    return View(am);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Zie de geselecteerde gebruiker in.
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>View</returns>
        public IActionResult IndividualUsers(int id)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                AdminModel am = new AdminModel();
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2)
                {
                    am.User = dq.RetrieveUser(id);
                    return View(am);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Wijzig de status van een verhaal.
        /// </summary>
        /// <param name="type">Status</param>
        /// <param name="id">Story ID</param>
        /// <returns>View</returns>
        public IActionResult ModifyStory (int type, int id)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                AdminModel am = new AdminModel();
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2)
                {
                    if (type == 0)
                    {
                        // Read
                        bool result = dq.UpdateStoryStatus(id, 1);
                    }
                    else if (type == 1)
                    {
                        // Accept
                        bool result = dq.UpdateStoryStatus(id, 2);
                    }
                    else if (type == 2)
                    {
                        // Delete
                        bool result = dq.DeleteStory(id);
                    }

                    return RedirectToAction("StoryOverview", "Overview");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigeer naar de serverconfiguratie.
        /// </summary>
        /// <returns>View</returns>
        public IActionResult ServerConfiguration()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                AdminModel am = new AdminModel();
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2)
                {
                    ServerConfigModel scm = new ServerConfigModel();
                    scm.DbConnectionString = ConfigurationManager.AppSettings["connectionString"];
                    scm.DbUsername = ConfigurationManager.AppSettings["dbUsername"];
                    scm.DbPassword = ConfigurationManager.AppSettings["dbPassword"];

                    return View(scm);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Update web.config met nieuwe waarden.
        /// </summary>
        /// <returns>View</returns>
        public IActionResult UpdateConfig(ServerConfigModel scm)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                AdminModel am = new AdminModel();
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2)
                {
                    Configuration configuration = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);

                    configuration.AppSettings.Settings["connectionString"].Value = scm.DbConnectionString;
                    configuration.AppSettings.Settings["dbUsername"].Value = scm.DbUsername;
                    configuration.AppSettings.Settings["dbPassword"].Value = scm.DbPassword;

                    configuration.Save();
                    ConfigurationManager.RefreshSection("appSettings");

                    return RedirectToAction("ServerConfiguration", "Overview");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Reset de server.
        /// </summary>
        /// <returns>View</returns>
        public IActionResult ResetServer()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                AdminModel am = new AdminModel();
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2)
                {
                    // todo
                }
            }

            return RedirectToAction("Index", "Home");
        }
    }
}