using System;
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
        public IActionResult ScoreOverview(int id, int rank)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                ScoreOverviewModel som = new ScoreOverviewModel();

                som.Viewer = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (som.Viewer.RoleId == 1)
                {
                    som.User = dq.RetrieveUser(id);
                    som.Stories = dq.RetrieveStoriesOfUser(id);
                    som.Scores = dq.RetrieveScoresOfUser(id);
                    som.Rank = rank;
                }
                else
                {
                    som.User = dq.RetrieveUser(som.Viewer.Id);
                    som.Stories = dq.RetrieveStoriesOfUser(som.Viewer.Id);
                    som.Scores = dq.RetrieveScoresOfUser(som.Viewer.Id);
                    som.Rank = rank;
                }

                som.GenerateAverage();

                return View(som);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigeer naar het klasoverzicht
        /// </summary>
        /// <returns>View</returns>
        public IActionResult ClassOverview()
        {
            // login check
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                HubModel hm = new HubModel();
                hm.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (hm.User.RoleId == 0)
                {
                    return RedirectToAction("ScoreOverview", "Overview", new { id = hm.User.Id, rank = -1 });
                }
                else if (hm.User.RoleId == 1)
                {
                    // Haal data op
                    hm.UserList = dq.RetrieveUserListByGroup(hm.User.Group);
                    hm.GenerateAverage();

                    return View(hm);
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
                else if (tempUser.RoleId == 3)
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
                am.User = tempUser;

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
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));
                IndividualModel ism = new IndividualModel();

                if (tempUser.RoleId == 1)
                {
                    ism.User = dq.RetrieveUser(id);
                }
                else
                {
                    ism.User = tempUser;
                }

                ism.Score = dq.RetrieveScore(scoreId);

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
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));
                IndividualModel ism = new IndividualModel();

                if (tempUser.RoleId == 1)
                {
                    ism.User = dq.RetrieveUser(id);
                }
                else
                {
                    ism.User = tempUser;
                }

                ism.Story = dq.RetrieveStory(storyId);

                return View(ism);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigeer naar de CreateScore pagina
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>View</returns>
        public IActionResult CreateScore(int id)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 1)
                {
                    IndividualModel ism = new IndividualModel();
                    ism.User = dq.RetrieveUser(id);
                    ism.Score = new Classes.Score();

                    List<Classes.GameType> gameTypeList = new List<Classes.GameType>();
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 1, TypeName = "Werkelijk Waar?" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 2, TypeName = "Liegen" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 3, TypeName = "Overladen" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 4, TypeName = "Misleiden" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 5, TypeName = "Quizmaster" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 6, TypeName = "Verkoper" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 7, TypeName = "Detective" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 8, TypeName = "Sneller dan het licht" });
                    gameTypeList.Add(new Classes.GameType { TypeIndex = 9, TypeName = "De machine" });

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
        public IActionResult UploadScore(IndividualModel ism)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if teacher
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 1)
                {
                    ism.Score.CashAmount = Convert.ToDouble(ism.ScoreCashString);

                    string answerString = "";
                    foreach (string s in ism.ScoreAnswerArray)
                    {
                        answerString += s + " ";
                    }

                    ism.Score.Answers = answerString;
                    ism.Score.GameType = ism.GameTypeIndex;
                    bool results = dq.CreateScore(ism.Score);
                }

                return RedirectToAction("ScoreOverview", "Overview", new { id = ism.Score.OwnerId, rank = 0 });
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Zie het geselecteerde verhaal in.
        /// </summary>
        /// <param name="id">Story ID</param>
        /// <returns></returns>
        public IActionResult EditStoryStatus(int id, int status)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                AdminModel am = new AdminModel();
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2 || tempUser.RoleId == 3)
                {
                    if (status == -1)
                    {
                        dq.DeleteStory(id);
                    }
                    else
                    {
                        dq.UpdateStoryStatus(id, status);
                    }

                    return RedirectToAction("StoryOverview", "Overview");
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

                if (tempUser.RoleId == 2 || tempUser.RoleId == 3)
                {
                    am.User = dq.RetrieveUser(id);
                    //am.Group = dq.RetrieveGroup(am.User.Group);
                    //am.School = dq.RetrieveSchool(am.Group.SchoolId);

                    //List<Classes.School> tempSchoolList = dq.RetrieveSchools();
                    //List<Classes.Group> tempGroupList = dq.RetrieveGroupsOfSchool(am.Group.SchoolId); // edit later to include other schools

                    List<Classes.Role> roleList = new List<Classes.Role>();
                    //List<Classes.Group> groupList = new List<Classes.Group>();
                    //List<Classes.School> schoolList = new List<Classes.School>();

                    roleList.Add(new Classes.Role { RoleIndex = 0, RoleName = "Student" });
                    roleList.Add(new Classes.Role { RoleIndex = 1, RoleName = "Docent" });
                    roleList.Add(new Classes.Role { RoleIndex = 2, RoleName = "Beheerder" });
                    roleList.Add(new Classes.Role { RoleIndex = 3, RoleName = "Überbeheerder" });

                    //l.DebugToLog("[IndividualUser]", "Forming group list.", 0);

                    //foreach(Classes.Group group in tempGroupList)
                    //{
                    //    groupList.Add(new Classes.Group { GroupId = group.Id, GroupName = group.GroupName });
                    //    l.DebugToLog("[IndividualUser]", "Group " + group.Id + " - " + group.GroupName + ".", 1);
                    //}

                    //l.DebugToLog("[IndividualUser]", "Forming school list.", 1);

                    //foreach (Classes.School school in tempSchoolList)
                    //{
                    //    schoolList.Add(new Classes.School { Id = school.Id, SchoolName = school.SchoolName });
                    //    l.DebugToLog("[IndividualUser]", "School " + school.Id + " - " + school.SchoolName + ".", 1);
                    //}

                    //l.DebugToLog("[IndividualUser]", "Done.", 2);

                    am.RoleList = new SelectList(roleList, "RoleIndex", "RoleName");
                    //am.GroupList = new SelectList(groupList, "GroupId", "GroupName");
                    //am.SchoolList = new SelectList(schoolList, "Id", "SchoolName");

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