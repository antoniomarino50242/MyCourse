using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MyCourse.Models.Options;

namespace MyCourse.Models.Services.Infrastructure
{
    public class MagickNetImagePersister : IImagePersister
    {
        private readonly IWebHostEnvironment env;

        private readonly SemaphoreSlim semaphore;
        private readonly IOptionsMonitor<ImageOptions> imageOptions;

        public MagickNetImagePersister(IWebHostEnvironment env, IOptionsMonitor<ImageOptions> imageOption)
        {
            this.imageOptions = imageOption;
            ResourceLimits.Height = 4000;
            ResourceLimits.Width = 4000;
            semaphore = new SemaphoreSlim(2);
            this.env = env;
        }

        public async Task<string> SaveCourseImageAsync(int courseId, IFormFile formFile)
        {
            //Il metodo WaitAsync ha anche un overload che permette di passare un timeout
            //Ad esempio, se vogliamo aspettare al massimo 1 secondo:
            //await semaphore.AwaitAsync(TimeSpan.FromSeconds(1));
            //Se il timeout scade, il SemaphoreSlim solleverà un'eccezione (così almeno non resta in attesa all'infinito)
            await semaphore.WaitAsync();
            try
            {
                //Salvare il file
                string path = $"/Courses/{courseId}.jpg";
                string physicalPath = Path.Combine(env.WebRootPath, "Courses", $"{courseId}.jpg");

                using Stream inputStream = formFile.OpenReadStream();
                using MagickImage image = new MagickImage(inputStream);

                //Manipolare l'immagine
                int width = imageOptions.CurrentValue.Default;  //Esercizio: ottenere questi valori dalla configurazione
                int height = imageOptions.CurrentValue.Default;
                MagickGeometry resizeGeometry = new MagickGeometry(width, height)
                {
                    FillArea = true
                };
                image.Resize(resizeGeometry);
                image.Crop(width, width, Gravity.Northwest);

                image.Quality = 70;
                image.Write(physicalPath, MagickFormat.Jpg);

                //Restituire il percorso al file
                return path;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}