using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class Score
    {
        /// <summary>
        /// Score ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Owner's User ID
        /// </summary>
        public int OwnerId { get; set; }

        /// <summary>
        /// GameType ID
        /// </summary>
        public int GameType { get; set; }

        /// <summary>
        /// Amount of acquired followers
        /// </summary>
        public int FollowerAmount { get; set; }

        /// <summary>
        /// Amount of acquired cash
        /// </summary>
        public double CashAmount { get; set; }

        /// <summary>
        /// Amount of attained votes
        /// </summary>
        public int AttainedVotes { get; set; }

        /// <summary>
        /// Amount of attained votes in the current round
        /// </summary>
        public int RoundVotes { get; set; }

        /// <summary>
        /// Submitted answers
        /// </summary>
        public string Answers { get; set; }

        /// <summary>
        /// Submitted answers that are correct
        /// </summary>
        public string CorrectAnswers { get; set; }

        /// <summary>
        /// Date of upload
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Current Owner's unique socket ID
        /// </summary>
        public string SocketId { get; set; }
    }
}
