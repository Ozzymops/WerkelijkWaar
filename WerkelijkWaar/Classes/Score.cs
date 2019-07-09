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
        public int FollowerAmount { get; set; } = 50;

        /// <summary>
        /// Amount of acquired / lost followers in one round. Temporary variable
        /// </summary>
        public int FollowerDelta { get; set; }

        /// <summary>
        /// Amount of acquired cash
        /// </summary>
        public double CashAmount { get; set; }

        /// <summary>
        /// Amount of acquired / lost cash in one round. Temporary variable
        /// </summary>
        public double CashDelta { get; set; }

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
        /// Last submitted result, correct or false?
        /// </summary>
        public bool LastResult { get; set; } = false;

        /// <summary>
        /// Generated score based on FollowerAmount and CashAmount
        /// </summary>
        public int ActualScore { get; set; }

        /// <summary>
        /// Date of upload
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Current Owner's unique connection ID
        /// </summary>
        public string ConnectionId { get; set; }
    }
}
