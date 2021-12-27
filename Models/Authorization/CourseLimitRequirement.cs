using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MyCourse.Models.Authorization
{
    public class CourseLimitRequirement : IAuthorizationRequirement
    {
        public CourseLimitRequirement(int limit)
        {
            Limit = limit;
        }

        public int Limit { get; }
    }
}