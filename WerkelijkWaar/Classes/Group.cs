using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Classes
{
    public class Group
    {
        /// <summary>
        /// Group ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// School ID of which the Group is a part of
        /// </summary>
        public int SchoolId { get; set; }

        /// <summary>
        /// Group ID (in school)
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Group name
        /// </summary>
        public string GroupName { get; set; }
    }
}
