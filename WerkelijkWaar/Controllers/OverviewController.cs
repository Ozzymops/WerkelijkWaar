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
        // Standard classes
        Classes.Logger logger = new Classes.Logger();
        Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

        /// <summary>
        /// Navigate to ScoreOverview
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="rank">User rank</param>
        /// <returns>View</returns>
        public IActionResult ScoreOverview(int userId, int rank)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                ScoreOverviewModel scoreOverviewModel = new ScoreOverviewModel();

                scoreOverviewModel.Viewer = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (scoreOverviewModel.Viewer.RoleId == 1)
                {
                    scoreOverviewModel.User = dq.RetrieveUser(userId);
                    scoreOverviewModel.Stories = dq.RetrieveStoriesOfUser(userId);
                    scoreOverviewModel.Scores = dq.RetrieveScoresOfUser(userId);
                    scoreOverviewModel.Rank = rank;
                }
                else
                {
                    scoreOverviewModel.User = dq.RetrieveUser(scoreOverviewModel.Viewer.Id);
                    scoreOverviewModel.Stories = dq.RetrieveStoriesOfUser(scoreOverviewModel.Viewer.Id);
                    scoreOverviewModel.Scores = dq.RetrieveScoresOfUser(scoreOverviewModel.Viewer.Id);
                    scoreOverviewModel.Rank = rank;
                }

                scoreOverviewModel.GenerateAverage();

                return View(scoreOverviewModel);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigate to ClassOverview
        /// </summary>
        /// <returns>View</returns>
        public IActionResult ClassOverview()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                HubModel hubModel = new HubModel();
                hubModel.User = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (hubModel.User.RoleId == 0)
                {
                    return RedirectToAction("ScoreOverview", "Overview", new { id = hubModel.User.Id, rank = -1 });
                }
                else if (hubModel.User.RoleId == 1)
                {
                    hubModel.UserList = dq.RetrieveUserListByGroup(hubModel.User.Group);
                    hubModel.GenerateAverage();

                    return View(hubModel);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigate to UserOverview
        /// </summary>
        /// <returns>View</returns>
        public IActionResult UserOverview()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                AdminModel adminModel = new AdminModel();
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2)
                {
                    adminModel.UserList = dq.RetrieveAllUsers();
                    return View(adminModel);
                }
                else if (tempUser.RoleId == 3)
                {
                    adminModel.UserList = dq.RetrieveAllUsers();
                    return View(adminModel);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigate to StoryOverview
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
        /// Navigate to IndividualScores
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="rank">Rank</param>
        /// <param name="scoreId">Score ID</param>
        /// <returns>View</returns>
        public IActionResult IndividualScores(int userId, int rank, int scoreId)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));
                IndividualModel individualModel = new IndividualModel();

                if (tempUser.RoleId == 1)
                {
                    individualModel.User = dq.RetrieveUser(userId);
                }
                else
                {
                    individualModel.User = tempUser;
                }

                individualModel.Score = dq.RetrieveScore(scoreId);

                return View(individualModel);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigate to IndividualStories
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="rank">Ranknummer</param>
        /// <param name="storyId">Story ID</param>
        /// <returns>View</returns>
        public IActionResult IndividualStories(int userId, int rank, int storyId)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));
                IndividualModel individualModel = new IndividualModel();

                if (tempUser.RoleId == 1)
                {
                    individualModel.User = dq.RetrieveUser(userId);
                }
                else
                {
                    individualModel.User = tempUser;
                }

                individualModel.Story = dq.RetrieveStory(storyId);

                return View(individualModel);
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigate to CreateScore
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>View</returns>
        public IActionResult CreateScore(int userId)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 1)
                {
                    IndividualModel individualModel = new IndividualModel();
                    individualModel.User = dq.RetrieveUser(userId);
                    individualModel.Score = new Classes.Score();

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

                    individualModel.GameType = new SelectList(gameTypeList, "TypeIndex", "TypeName");

                    return View(individualModel);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Upload a score to the database
        /// </summary>
        /// <param name="individualModel">IndividualModel</param>
        /// <returns>View</returns>
        public IActionResult UploadScore(IndividualModel individualModel)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if teacher
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 1)
                {
                    individualModel.Score.CashAmount = Convert.ToDouble(individualModel.ScoreCashString);

                    string answerString = "";

                    foreach (string s in individualModel.ScoreAnswerArray)
                    {
                        answerString += s + " ";
                    }

                    individualModel.Score.Answers = answerString;
                    individualModel.Score.GameType = individualModel.GameTypeIndex;
                    bool results = dq.CreateScore(individualModel.Score);
                }

                return RedirectToAction("ScoreOverview", "Overview", new { scoreId = individualModel.Score.OwnerId, rank = 0 });
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Update story status
        /// </summary>
        /// <param name="storyId">Story ID</param>
        /// <returns>View</returns>
        public IActionResult EditStoryStatus(int storyId, int status)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2 || tempUser.RoleId == 3)
                {
                    if (status == -1)
                    {
                        dq.DeleteStory(storyId);
                    }
                    else
                    {
                        dq.UpdateStoryStatus(storyId, status);
                    }

                    return RedirectToAction("StoryOverview", "Overview");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigate to IndividualUsers
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>View</returns>
        public IActionResult IndividualUsers(int userId)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                AdminModel adminModel = new AdminModel();
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2 || tempUser.RoleId == 3)
                {
                    adminModel.User = dq.RetrieveUser(userId);
                    //adminModel.Group = dq.RetrieveGroup(adminModel.User.Group);
                    //adminModel.School = dq.RetrieveSchool(adminModel.Group.SchoolId);

                    //List<Classes.School> tempSchoolList = dq.RetrieveSchools();
                    //List<Classes.Group> tempGroupList = dq.RetrieveGroupsOfSchool(adminModel.Group.SchoolId); // edit later to include other schools

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

                    adminModel.RoleList = new SelectList(roleList, "RoleIndex", "RoleName");
                    //adminModel.GroupList = new SelectList(groupList, "GroupId", "GroupName");
                    //adminModel.SchoolList = new SelectList(schoolList, "Id", "SchoolName");

                    return View(adminModel);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Navigate to ServerConfiguration
        /// </summary>
        /// <returns>View</returns>
        public IActionResult ServerConfiguration()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2)
                {
                    ServerConfigModel serverConfigModel = new ServerConfigModel();
                    serverConfigModel.DbConnectionString = ConfigurationManager.AppSettings["connectionString"];
                    serverConfigModel.DbUsername = ConfigurationManager.AppSettings["dbUsername"];
                    serverConfigModel.DbPassword = ConfigurationManager.AppSettings["dbPassword"];

                    return View(serverConfigModel);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Update web.config with new attributes [w.i.p.]
        /// </summary>
        /// <returns>View</returns>
        public IActionResult UpdateConfig(ServerConfigModel serverConfigModel)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
                Classes.User tempUser = Newtonsoft.Json.JsonConvert.DeserializeObject<Classes.User>(HttpContext.Session.GetString("User"));

                if (tempUser.RoleId == 2)
                {
                    Configuration configuration = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);

                    configuration.AppSettings.Settings["connectionString"].Value = serverConfigModel.DbConnectionString;
                    configuration.AppSettings.Settings["dbUsername"].Value = serverConfigModel.DbUsername;
                    configuration.AppSettings.Settings["dbPassword"].Value = serverConfigModel.DbPassword;

                    configuration.Save();
                    ConfigurationManager.RefreshSection("appSettings");

                    return RedirectToAction("ServerConfiguration", "Overview");
                }
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Reset server [w.i.p.]
        /// </summary>
        /// <returns>View</returns>
        public IActionResult ResetServer()
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                // Check if administrator
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