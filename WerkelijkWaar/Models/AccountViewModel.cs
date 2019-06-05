using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class AccountViewModel
    {
        /// <summary>
        /// User
        /// </summary>
        public Classes.User User { get; set; }

        /// <summary>
        /// Status string
        /// </summary>
        public string StatusString { get; set; }

        /// <summary>
        /// 0 - above Identiteit, 1 - above Veiligheid, 2 - above Avatar
        /// </summary>
        public int StatusLocation = -1;

        /// <summary>
        /// Old password for confirmation
        /// </summary>
        public string OldPassword { get; set; }

        /// <summary>
        /// New password
        /// </summary>
        public string NewPassword { get; set; }

        /// <summary>
        /// New password for confirmation
        /// </summary>
        public string NewPasswordConfirmation { get; set; }

        /// <summary>
        /// Uploaded avatar file
        /// </summary>
        public IFormFile UploadedAvatar { get; set; }
    }
}
