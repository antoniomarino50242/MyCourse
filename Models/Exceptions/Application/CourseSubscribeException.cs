using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourse.Models.Exceptions.Application
{
    public class CourseSubscribeException : Exception
    {
        public CourseSubscribeException(int courseId) : base($"Couldn't subscribe to course {courseId}")
        {
        }
    }
}