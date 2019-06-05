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
        public string StatusString { get; set; }

        public Classes.User User { get; set; }
        public List<Classes.User> UserList { get; set; }
        public IFormFile UploadedAvatar { get; set; }

        public Classes.Story Story { get; set; }
        public List<Classes.Story> StoryList { get; set; }

        public Classes.School School { get; set; }
        public List<SelectListItem> schoolListItems = new List<SelectListItem>();

        public Classes.Group Group { get; set; }
        public List<SelectListItem> groupListItems = new List<SelectListItem>();

        public SelectList RoleList { get; set; }
        public SelectList SchoolList { get; set; }
        public SelectList GroupList { get; set; }

        public List<string> LogEntries { get; set; }

        public void GetLogEntries()
        {

        }
    }
}
