using System.Collections.Generic;
using System.Threading.Tasks;
using MyCourse.Models.InputModels;
using MyCourse.Models.ViewModels;

namespace MyCourse.Models.Services.Application
{
    public interface ICourseService
    {
         Task<List<CourseViewModel>> GetCoursesAsync(CourseListInputModel input);
         Task<CourseDetailViewModel> GetCourseAsync(int id);
    }
}