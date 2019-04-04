using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class RegisterModel
    {
        [Required]
        public int RoleId { get; set; }

        [Required]
        public int Code { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [RegularExpression(@"[^a-zA-Z0-9_.]")]
        [Required]
        public string Name { get; set; }

        [RegularExpression(@"[^a-zA-Z0-9_.]")]
        [Required]
        public string Surname { get; set; }
        public string ImageSource { get; set; }

        // Privacy policy
        [Required]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Je moet akkoord gaan met de voorwaarden.")]
        public bool AgeCheck { get; set; }

        [Required]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Je moet akkoord gaan met de voorwaarden.")]
        public bool PrivacyCheck { get; set; }
    }
}
