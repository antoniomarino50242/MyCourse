using MyCourse.Models.InputModels.Courses;

namespace MyCourse.Models.ViewModels.Courses
{
    public class CourseListViewModel : IPaginationInfo
    {
        public ListViewModel<CourseViewModel> Courses { get; set; }
        public CourseListInputModel Input { get; set; }

        #region Implementazione IPaginationInfo
        public int CurrentPage => Input.Page;

        public int TotalResult => Courses.TotalCount;

        public int ResultsPerPage => Input.Limit;

        public string Search => Input.Search;

        public string OrderBy => Input.OrderBy;

        public bool Ascending => Input.Ascending;
        #endregion
    }
}