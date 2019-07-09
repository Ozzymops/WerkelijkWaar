using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class Story
    {
        /// <summary>
        /// Story ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Owner's User ID
        /// </summary>
        public int OwnerId { get; set; }

        /// <summary>
        /// Current Owner's unique connection ID
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Is the story a root story?
        /// </summary>
        public bool IsRoot { get; set; }

        /// <summary>
        /// Story title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Story itself
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Date of upload
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Story status
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Story status
        /// </summary>
        public int Source { get; set; }

        // Game
        /// <summary>
        /// Amount of attained votes
        /// </summary>
        public int Votes { get; set; }

        /// <summary>
        /// Assigned game group
        /// </summary>
        public int GameGroup { get; set; }

        /// <summary>
        /// Is the 'double score when chosen' power-up active?
        /// </summary>
        public bool PowerupActive { get; set; }
    }
}
