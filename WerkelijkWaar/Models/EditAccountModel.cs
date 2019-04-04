using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class EditAccountModel
    {
        public Classes.User User { get; set; }
        public string StatusString { get; set; }

        // New data
        public int Id { get; set; }

        [RegularExpression(@"[^a-zA-Z0-9_.]")]
        public string NewName { get; set; }

        [RegularExpression(@"[^a-zA-Z0-9_.]")]
        public string NewSurname { get; set; }
        public string NewUsername { get; set; }
        public string NewPassword { get; set; }
        public string NewAvatar { get; set; }
    }
}
