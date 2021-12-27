using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MyCourse.Models.Services.Application.Courses;

namespace MyCourse.Models.Authorization
{
    public class CourseLimitRequirementHandler : AuthorizationHandler<CourseLimitRequirement>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICachedCourseService courseService;

        public CourseLimitRequirementHandler(IHttpContextAccessor httpContextAccessor, ICachedCourseService courseService)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.courseService = courseService;
        }
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CourseLimitRequirement requirement)
        {
            //Legere l'id dell'utete dalla sua identità
            string userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Estrarre dal db i corsi creati dall'utente
            int courseCount = await courseService.GetCourseCountByAuthorIdAsync(userId);

            //Verificare se il numero dei corsi è minore di 5
            if (courseCount <= requirement.Limit)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }
}