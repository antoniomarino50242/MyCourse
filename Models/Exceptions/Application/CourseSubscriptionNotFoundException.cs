using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourse.Models.Exceptions.Application
{
    public class CourseSubscriptionNotFoundException : Exception
    {
        public CourseSubscriptionNotFoundException(int courseId) : base($"Subscriptions to course {courseId} not found")
        {
        }
    }
}