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
    }
}
