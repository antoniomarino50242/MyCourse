using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourse.Models.Options
{
    public class PaypalOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public bool IsSandbox { get; set; }
        public string BrandName { get; set; }
    }
}