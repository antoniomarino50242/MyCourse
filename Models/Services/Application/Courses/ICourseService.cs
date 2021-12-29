using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MyCourse.Models.Authorization;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.ViewModels;
using MyCourse.Models.ViewModels.Courses;

namespace MyCourse.Models.Services.Application.Courses
{
    public interface ICourseService
    {
        Task<ListViewModel<CourseViewModel>> GetCoursesAsync(CourseListInputModel model);
        Task<CourseDetailViewModel> GetCourseAsync(int id);
        Task<List<CourseViewModel>> GetMostRecentCoursesAsync();
        Task<List<CourseViewModel>> GetBestRatingCoursesAsync();
        Task<CourseEditInputModel> GetCourseForEditingAsync(int id);
        Task<CourseDetailViewModel> CreateCourseAsync(CourseCreateInputModel inputModel);
        Task<bool> IsTitleAvailableAsync(string title, int excludeId);
        Task<CourseDetailViewModel> EditCourseAsync(CourseEditInputModel inputModel);
        Task DeleteCourseAsync(CourseDeleteInputModel inputModel);
        Task SendQuestionToCourseAuthorAsync(int courseId, string question);
        Task<string> GetCourseAuthorIdAsync(int courseId);
        Task<int> GetCourseCountByAuthorIdAsync(string userId);
        Task SubscribeCourseAsync(CourseSubscribeInputModel inputModel);
        Task<bool> IsCourseSubscribedAsync(int courseId, string userId);
        Task<CourseSubscribeInputModel> CapturePaymentAsync(int id, string token);
        Task<string> GetPaymentUrlAsync(int id);
    }
}