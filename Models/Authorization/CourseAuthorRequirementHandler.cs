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
    public class CourseAuthorRequirementHandler : AuthorizationHandler<CourseAuthorRequirement>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICachedCourseService courseService;

        public CourseAuthorRequirementHandler(IHttpContextAccessor httpContextAccessor, ICachedCourseService courseService)
        {
            this.courseService = courseService;
            this.httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CourseAuthorRequirement requirement)
        {
            //Leggere l'id dell'utente dalla sua identità
            string userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Capire a quale corso sta cercando di accedere
            int courseId = Convert.ToInt32(httpContextAccessor.HttpContext.Request.RouteValues["id"]);
            if (courseId == 0)
            {
                context.Fail();
                return;
            }
            //Estrarre dal db l'id dell'autore del corso
            string authorId = await courseService.GetCourseAuthorIdAsync(courseId);

            //Verificare che l'id dell'utnete sia uguale all'id dell'autore del corso
            if (userId == authorId)
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