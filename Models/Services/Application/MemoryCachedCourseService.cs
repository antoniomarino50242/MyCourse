using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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

        public Task<CourseDetailViewModel> GetCourseAsync(int id)
        {
            return memoryCache.GetOrCreateAsync($"Course{id}", cacheEntry =>
            {
                cacheEntry.SetSize(1);
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeOptions.CurrentValue.Default));
                return courseService.GetCourseAsync(id);
            });
        }

        public Task<List<CourseViewModel>> GetCoursesAsync(string search, int page, string orderBy, bool ascending)
        {
            return memoryCache.GetOrCreateAsync($"Courses{search}-{page}-{orderBy}-{ascending}", cacheEntry =>
            {
                cacheEntry.SetSize(1);
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeOptions.CurrentValue.Default));
                return courseService.GetCoursesAsync(search, page, orderBy, ascending);
            });
        }
    }
}