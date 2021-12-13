using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MyCourse.Models.InputModels.Lessons;
using MyCourse.Models.Options;
using MyCourse.Models.ViewModels.Lessons;

namespace MyCourse.Models.Services.Application.Lessons
{
    public class MemoryCachedLessonService : ICachedLessonService
    {
        private readonly ILessonService lessonService;
        private readonly IMemoryCache memoryCache;
        private readonly IOptionsMonitor<TimeOptions> timeOptions;
        public MemoryCachedLessonService(ILessonService lessonService, IMemoryCache memoryCache, IOptionsMonitor<TimeOptions> timeOptions)
        {
            this.timeOptions = timeOptions;
            this.memoryCache = memoryCache;
            this.lessonService = lessonService;

        }

        public Task<LessonDetailViewModel> GetLessonAsync(int id)
        {
            return memoryCache.GetOrCreateAsync($"Lesson{id}", cacheEntry =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeOptions.CurrentValue.Default));
                return lessonService.GetLessonAsync(id);
            });
        }

        public async Task<LessonDetailViewModel> CreateLessonAsync(LessonCreateInputModel inputModel)
        {
            LessonDetailViewModel viewModel = await lessonService.CreateLessonAsync(inputModel);
            memoryCache.Remove($"Lesson{viewModel.CourseId}");
            return viewModel;
        }

        public async Task<LessonDetailViewModel> EditLessonAsync(LessonEditInputModel inputModel)
        {
            LessonDetailViewModel viewModel = await lessonService.EditLessonAsync(inputModel);
            memoryCache.Remove($"Course{viewModel.CourseId}");
            memoryCache.Remove($"Lesson{viewModel.Id}");
            return viewModel;
        }


        public Task<LessonEditInputModel> GetLessonForEditingAsync(int id)
        {
            return lessonService.GetLessonForEditingAsync(id);
        }

        public async Task DeleteLessonAsync(LessonDeleteInputModel inputModel)
        {
            await lessonService.DeleteLessonAsync(inputModel);
            memoryCache.Remove($"Course{inputModel.CourseId}");
            memoryCache.Remove($"Lesson{inputModel.Id}");
        }
    }
}