using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class RegisterModel
    {
        /// <summary>
        /// Status string
        /// </summary>
        public string StatusString { get; set; }

        /// <summary>
        /// Role ID
        /// </summary>
        [Required]
        public int RoleId { get; set; }

        /// <summary>
        /// School ID
        /// </summary>
        [Required]
        public string School { get; set; }

        /// <summary>
        /// Group ID
        /// </summary>
        [Required]
        public int Group { get; set; }

        /// <summary>
        /// Username
        /// </summary>
        [Required]
        public string Username { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [RegularExpression(@"[^a-zA-Z0-9_.]")]
        [Required]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Jouw voornaam kan geen vreemde tekens bevatten.")]
        public string Name { get; set; }

        /// <summary>
        /// Surname
        /// </summary>
        [RegularExpression(@"[^a-zA-Z0-9_.]")]
        [Required]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Jouw achternaam kan geen vreemde tekens bevatten.")]
        public string Surname { get; set; }

        /// <summary>
        /// Avatar filename
        /// </summary>
        public string ImageSource { get; set; }

        // Privacy policy
        /// <summary>
        /// Privacy policy check
        /// </summary>
        [Required]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Je moet akkoord gaan met de voorwaarden.")]
        public bool PrivacyCheck { get; set; }
    }
}
