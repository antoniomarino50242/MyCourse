using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MyCourse.Models.InputModels;
using MyCourse.Models.Options;
using MyCourse.Models.ViewModels;

namespace MyCourse.Models.Services.Application
{
    public class MemoryCachedCourseService : ICachedCourseService
    {
        private readonly ICourseService courseService;
        private readonly IMemoryCache memoryCache;
        private readonly IOptionsMonitor<TimeOptions> timeOptions;

        public MemoryCachedCourseService(ICourseService courseService, IMemoryCache memoryCache, IOptionsMonitor<TimeOptions> timeOptions)
        {
            this.timeOptions = timeOptions;
            this.memoryCache = memoryCache;
            this.courseService = courseService;
        }

        public Task<CourseDetailViewModel> CreateCourseAsync(CourseCreateInputModel inputModel)
        {
            return courseService.CreateCourseAsync(inputModel);
        }



        public Task<List<CourseViewModel>> GetBestRatingCoursesAsync()
        {
            return memoryCache.GetOrCreateAsync($"BestRatingCourses", cacheEntry =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeOptions.CurrentValue.Default));
                return courseService.GetBestRatingCoursesAsync();
            });
        }

        public Task<CourseDetailViewModel> GetCourseAsync(int id)
        {
            return memoryCache.GetOrCreateAsync($"Course{id}", cacheEntry =>
            {
                //cacheEntry.SetSize(1);
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeOptions.CurrentValue.Default));
                return courseService.GetCourseAsync(id);
            });
        }

        

        public Task<ListViewModel<CourseViewModel>> GetCoursesAsync(CourseListInputModel model)
        {
            //Metto in cache i risultati solo per le prime 5 pagine del catalogo, che reputo essere
            //le più visitate dagli utenti, e che perciò mi permettono di avere il maggior beneficio dalla cache.
            //E inoltre, metto in cache i risultati solo se l'utente non ha cercato nulla.
            //In questo modo riduco drasticamente il consumo di memoria RAM
            bool canCache = model.Page <= 5 && string.IsNullOrEmpty(model.Search);
            
            //Se canCache è true, sfrutto il meccanismo di caching
            if (canCache)
            {
                return memoryCache.GetOrCreateAsync($"Courses{model.Page}-{model.OrderBy}-{model.Ascending}", cacheEntry => 
                {
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(60));
                    return courseService.GetCoursesAsync(model);
                });
            }

            //Altrimenti uso il servizio applicativo sottostante, che recupererà sempre i valori dal database
            return courseService.GetCoursesAsync(model);
        }

        public Task<List<CourseViewModel>> GetMostRecentCoursesAsync()
        {
            return memoryCache.GetOrCreateAsync($"MostRecentCourses", cacheEntry => 
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeOptions.CurrentValue.Default));
                return courseService.GetMostRecentCoursesAsync();
            });
        }

        public Task<bool> IsTitleAvailableAsync(string title)
        {
            return courseService.IsTitleAvailableAsync(title);
        }

        public Task<CourseEditInputModel> GetCourseForEditingAsync(int id)
        {
            return courseService.GetCourseForEditingAsync(id);
        }
        
        public Task<CourseDetailViewModel> EditCourseAsync(CourseEditInputModel inputModel)
        {
            return courseService.EditCourseAsync(inputModel);
        }  
    }
}