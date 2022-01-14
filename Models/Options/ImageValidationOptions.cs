using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourse.Models.Options
{
    public class ImageValidationOptions
    {
        public string Key { get; set; }
        public string Endpoint { get; set; }
        public float MaximumAdultScore { get; set; }
    }
}