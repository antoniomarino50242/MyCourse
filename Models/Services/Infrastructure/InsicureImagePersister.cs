using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace MyCourse.Models.Services.Infrastructure
{
    public class InsicureImagePersister : IImagePersister
    {
        private readonly IWebHostEnvironment env;

        public InsicureImagePersister(IWebHostEnvironment env)
        {
            this.env = env;
        }

        public async Task<string> SaveCourseImageAsync(int courseId, IFormFile formFile)
        {
            //Salvare i file
            string path = $"/Courses/{courseId}.jpg";
            string physicalPath = Path.Combine(env.WebRootPath, "Courses", $"{courseId}.jpg");
            using FileStream fileStream = File.OpenWrite(physicalPath);
            await formFile.CopyToAsync(fileStream);

            //Restituire il percorso al file
            return path;
        }
    }
}