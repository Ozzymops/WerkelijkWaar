using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class ScoreOverviewModel
    {
        public Classes.User CurrentUser { get; set; }
        public List<Classes.User> UserList { get; set; }

        public double AverageScore { get; set; }

        public void GenerateAverage()
        {
            Classes.Logger l = new Classes.Logger();
            Classes.DatabaseQueries dq = new Classes.DatabaseQueries();


            l.DebugToLog("[GenerateAverage]", UserList.Count + " users.", 0);

            if (UserList != null && UserList.Count != 0)
            {
                l.DebugToLog("[GenerateAverage]", "Generating average for " + UserList.Count + " users.", 1);

                // Individual
                foreach (Classes.User user in UserList)
                {
                    List<Classes.Score> listOfScores = dq.RetrieveScoresOfUser(user.Id);

                    if (listOfScores != null && listOfScores.Count != 0)
                    {
                        int correctAnswers = 0;
                        int totalLengthOfAnswers = 0;

                        l.DebugToLog("[GenerateAverage]", "User " + user.Username + " has " + listOfScores.Count + " scores to his/her name.", 1);

                        foreach (Classes.Score score in listOfScores)
                        {
                            if (score.GameType == 1)
                            {
                                char[] digits = score.Answers.ToCharArray();

                                foreach (char digit in digits)
                                {
                                    if (digit == '1')
                                    {
                                        correctAnswers++;
                                    }

                                    totalLengthOfAnswers++;
                                }

                                l.DebugToLog("[GenerateAverage]", "Score " + score.Id + ": " + correctAnswers + " correct answers from here on out.", 1);
                            }
                        }

                        user.AverageScore = (correctAnswers / totalLengthOfAnswers) * 10;
                    }
                    else
                    {
                        user.AverageScore = 0.0;
                    }

                    l.DebugToLog("[GenerateAverage]", "Average score: " + user.AverageScore + ".\n---------", 1);
                }

                // Class
                double totalAverage = 0.0;

                foreach (Classes.User u in UserList)
                {
                    totalAverage += u.AverageScore;
                }

                AverageScore = (totalAverage / UserList.Count);
                l.DebugToLog("[GenerateAverage]", "Class average is " + AverageScore, 2);
            }
        }
    }
}
