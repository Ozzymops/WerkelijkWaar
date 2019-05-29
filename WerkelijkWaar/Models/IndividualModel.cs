using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class IndividualModel
    {
        public Classes.User User { get; set; }

        public Classes.Score Score { get; set; }
        public List<Classes.Score> Scores { get; set; }

        public Classes.Story Story { get; set; }
        public List<Classes.Story> Stories { get; set; }

        public int ScoreId { get; set; }
        public int StoryId { get; set; }
        public int Rank { get; set; }

        public int Role { get; set; }

        public int GameTypeIndex { get; set; }
        public SelectList GameType { get; set; }

        public void GetUser(int id)
        {
            Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

            User = dq.RetrieveUser(id);
        }

        public void GenerateAverage()
        {
            Classes.Logger l = new Classes.Logger();
            Classes.DatabaseQueries dq = new Classes.DatabaseQueries();

            List<Classes.Score> listOfScores = dq.RetrieveScoresOfUser(User.Id);

            if (listOfScores != null && listOfScores.Count != 0)
            {
                l.DebugToLog("[GenerateAverage]", User.Username + " has " + listOfScores.Count + " scores.", 0);

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

            l.DebugToLog("[GenerateAverage]", User.Username + "'s average is " + User.AverageScore, 2);
        }
    }
}
