using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WerkelijkWaar.Models;

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
                    // redirect to individual screen
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
                ism.Scores = dq.RetrieveScoresOfUser(ism.User.Id);
                ism.Rank = rank;

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

                return View(ism);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult IndividualStories(int id, int rank, int storyId)
        {
            // Check if logged in
            if (!String.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                IndividualModel ism = new IndividualModel();
                ism.GetUser(id);
                ism.Stories = dq.RetrieveStoriesOfUser(ism.User.Id);
                ism.Rank = rank;

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

        public IActionResult Configuration()
        {
            return View();
        }
    }
}