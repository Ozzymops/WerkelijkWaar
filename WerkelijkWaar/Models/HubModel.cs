using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class HubModel
    {
        Classes.Logger logger = new Classes.Logger();
        Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

        /// <summary>
        /// User
        /// </summary>
        public Classes.User User { get; set; }

        /// <summary>
        /// List of Users
        /// </summary>
        public List<Classes.User> UserList { get; set; }

        /// <summary>
        /// Average score of all scores in class
        /// </summary>
        public double AverageScore { get; set; }

        /// <summary>
        /// Generate the average class score
        /// </summary>
        public void GenerateAverage()
        {
            if (UserList != null && UserList.Count != 0)
            {
                // Individual
                foreach (Classes.User user in UserList)
                {
                    List<Classes.Score> listOfScores = dq.RetrieveScoresOfUser(user.Id);

                    if (listOfScores != null && listOfScores.Count != 0)
                    {
                        decimal correctAnswers = 0;
                        decimal totalLengthOfAnswers = 0;

                        foreach (Classes.Score score in listOfScores)
                        {
                            if (score.GameType == 1)
                            {
                                char[] digits = score.Answers.ToCharArray();

                                foreach (char digit in digits)
                                {
                                    if (digit == '1')
                                    {
                                        correctAnswers += 1;
                                    }

                                    totalLengthOfAnswers += 1;
                                }
                            }
                        }

                        double tempAverage = (double)((correctAnswers / totalLengthOfAnswers) * 10);
                        user.AverageScore = tempAverage;
                    }
                    else
                    {
                        user.AverageScore = 0.0;
                    }
                }

                // Class
                double totalAverage = 0.0;

                foreach (Classes.User u in UserList)
                {
                    totalAverage += u.AverageScore;
                }

                AverageScore = (totalAverage / UserList.Count);
            }
        }
    }
}
