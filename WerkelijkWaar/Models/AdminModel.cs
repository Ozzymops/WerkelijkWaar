using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class AdminModel
    {
        public List<Classes.User> UserList { get; set; }
        public Classes.User User { get; set; }
        public List<Classes.Story> StoryList { get; set; }
        public Classes.Story Story { get; set; }
        public Classes.School School { get; set; }
        public List<Classes.School> SchoolList { get; set; }
        public List<SelectListItem> schoolListItems = new List<SelectListItem>();

        public List<string> LogEntries { get; set; }

        public void GenerateSchoolListItems()
        {
            foreach (Classes.School school in SchoolList)
            {
                schoolListItems.Add(new SelectListItem { Text = school.SchoolName + " (" + school.Id + ")", Value = school.Id.ToString() });
            }

            schoolListItems[0].Selected = true;
        }

        public void GetLogEntries()
        {

        }
    }
}
