using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class Score
    {
        public int Id { get; set; }

        // OwnerId = User.Id
        public int OwnerId { get; set; }
        public int GameType { get; set; }
        public int FollowerAmount { get; set; }
        public double CashAmount { get; set; }
        public int AttainedVotes { get; set; }
        public int RoundVotes { get; set; }
        public string Answers { get; set; }
        public string CorrectAnswers { get; set; }
        public DateTime Date { get; set; }
        public string SocketId { get; set; }
    }
}
