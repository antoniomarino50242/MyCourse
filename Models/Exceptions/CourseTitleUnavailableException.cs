using System;

namespace MyCourse.Models.Exceptions
{
    public class CourseTitleUnavailableException : Exception
    {
        public CourseTitleUnavailableException(string title, Exception innerExeption): base($"Course title '{title}' existed", innerExeption)
        {
        }
    }
}