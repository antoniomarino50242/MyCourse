using System;

namespace MyCourse.Models.Exceptions.Infrastructure
{
    public class ImagePersistenceException : Exception
    {
        public ImagePersistenceException(Exception innerExeption) : base("Couldn't persist the image", innerExeption)
        {
        }
    }
}