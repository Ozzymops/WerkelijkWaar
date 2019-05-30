using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class ScoreOverviewModel
    {
        public Classes.User Viewer { get; set; }
        public Classes.User User { get; set; }
        public List<Classes.Story> Stories { get; set; }
        public List<Classes.Score> Scores { get; set; }
        public int Rank { get; set; }

        public void GenerateAverage()
        {
            // Classes.Logger l = new Classes.Logger();
            Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

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
    }
}
