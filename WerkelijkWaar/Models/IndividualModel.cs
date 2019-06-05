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
        /// <summary>
        /// User
        /// </summary>
        public Classes.User User { get; set; }

        /// <summary>
        /// Score
        /// </summary>
        public Classes.Score Score { get; set; } = new Classes.Score();

        /// <summary>
        /// Story
        /// </summary>
        public Classes.Story Story { get; set; }

        /// <summary>
        /// GameType ID
        /// </summary>
        public int GameTypeIndex { get; set; }

        /// <summary>
        /// GameType SelectList
        /// </summary>
        public SelectList GameType { get; set; }

        // Create score
        /// <summary>
        /// Score CashAmount as string
        /// </summary>
        public string ScoreCashString { get; set; }

        /// <summary>
        /// Score Answers as string[]
        /// </summary>
        public string[] ScoreAnswerArray { get; set; }
    }
}
