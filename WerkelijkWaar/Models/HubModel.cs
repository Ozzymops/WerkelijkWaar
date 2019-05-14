using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class HubModel
    {
        public Classes.User User { get; set; }
        public List<Classes.User> UserList { get; set; }

        public double AverageScore { get; set; }
        public void GenerateAverage()
        {
            if (UserList != null && UserList.Count != 0)
            {
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
