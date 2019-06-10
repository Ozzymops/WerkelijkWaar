using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class ScoreOverviewModel
    {
        /// <summary>
        /// User that is viewing the page
        /// </summary>
        public Classes.User Viewer { get; set; }

        /// <summary>
        /// User that is being displayed on page
        /// </summary>
        public Classes.User User { get; set; }

        /// <summary>
        /// List of Stories by User
        /// </summary>
        public List<Classes.Story> Stories { get; set; }

        /// <summary>
        /// List of Scores by User
        /// </summary>
        public List<Classes.Score> Scores { get; set; }

        /// <summary>
        /// User rank
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Generate the average class score
        /// </summary>
        public void GenerateAverage()
        {
            Classes.Logger logger = new Classes.Logger();
            Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

            try
            {
                logger.Log("[HubModel - GenerateAverage]", "Calculating AverageScore...", 0, 2, false);

                if (Scores != null && Scores.Count != 0)
                {
                    decimal correctAnswers = 0;
                    decimal totalLengthOfAnswers = 0;

                    foreach (Classes.Score score in Scores)
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
                        }
                    }

                    User.AverageScore = (double)((correctAnswers / totalLengthOfAnswers) * 10);
                }
                else
                {
                    User.AverageScore = 0.0;
                }
            }
            catch (Exception exception)
            {
                logger.Log("[ScoreOverviewModel - GenerateAverage]", "Something went wrong:\n" + exception, 2, 2, false);
                User.AverageScore = 0.0;
            }           
        }            
    }
}
