using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class EditAccountModel
    {
        /// <summary>
        /// User
        /// </summary>
        public Classes.User User { get; set; }

        /// <summary>
        /// Status string
        /// </summary>
        public string StatusString { get; set; }

        // New data
        /// <summary>
        /// User ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// New User name
        /// </summary>
        [RegularExpression(@"[^a-zA-Z0-9_.]")]
        public string NewName { get; set; }

        /// <summary>
        /// New User surname
        /// </summary>
        [RegularExpression(@"[^a-zA-Z0-9_.]")]
        public string NewSurname { get; set; }

        /// <summary>
        /// New User username
        /// </summary>
        public string NewUsername { get; set; }

        /// <summary>
        /// New User password
        /// </summary>
        public string NewPassword { get; set; }

        /// <summary>
        /// New User avatar
        /// </summary>
        public IFormFile NewAvatar { get; set; }
    }
}
