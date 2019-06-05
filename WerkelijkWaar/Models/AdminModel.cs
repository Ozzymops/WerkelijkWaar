using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class AdminModel
    {
        /// <summary>
        /// Status string
        /// </summary>
        public string StatusString { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public Classes.User User { get; set; }

        /// <summary>
        /// List of Users
        /// </summary>
        public List<Classes.User> UserList { get; set; }

        /// <summary>
        /// Uploaded avatar file
        /// </summary>
        public IFormFile UploadedAvatar { get; set; }

        /// <summary>
        /// Story
        /// </summary>
        public Classes.Story Story { get; set; }

        /// <summary>
        /// List of Stories
        /// </summary>
        public List<Classes.Story> StoryList { get; set; }

        /// <summary>
        /// School
        /// </summary>
        public Classes.School School { get; set; }

        /// <summary>
        /// List of Schools
        /// </summary>
        public List<SelectListItem> schoolListItems = new List<SelectListItem>();

        /// <summary>
        /// Group
        /// </summary>
        public Classes.Group Group { get; set; }

        /// <summary>
        /// List of Groups
        /// </summary>
        public List<SelectListItem> groupListItems = new List<SelectListItem>();

        /// <summary>
        /// List of Roles
        /// </summary>
        public SelectList RoleList { get; set; }

        /// <summary>
        /// List of Schools
        /// </summary>
        public SelectList SchoolList { get; set; }

        /// <summary>
        /// List of Groups
        /// </summary>
        public SelectList GroupList { get; set; }

        /// <summary>
        /// List of Log entries
        /// </summary>
        public List<string> LogEntries { get; set; }

        /// <summary>
        /// Get log entries
        /// </summary>
        public void GetLogEntries()
        {

        }
    }
}
