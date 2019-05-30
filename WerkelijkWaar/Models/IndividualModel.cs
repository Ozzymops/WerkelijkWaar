using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WerkelijkWaar.Models
{
    public class IndividualModel
    {
        public Classes.User User { get; set; }

        public Classes.Score Score { get; set; }
        public Classes.Story Story { get; set; }

        public int GameTypeIndex { get; set; }
        public SelectList GameType { get; set; }
    }
}
