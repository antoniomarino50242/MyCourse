using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyCourse.Models.Services.Application.Courses;
using MyCourse.Models.ViewModels.Courses;

namespace MyCourse.Pages
{
    public class ContactModel : PageModel
    {
        public CourseDetailViewModel Course { get; private set;}

        public async Task<IActionResult> OnGetAsync(int id,[FromServices] ICourseService courseService)
        {   
            try
            {
                Course = await courseService.GetCourseAsync(id);
                ViewData["Title"] = $"Invia una domanda";
                return Page(); 
            }
            catch (System.Exception)
            {
                return RedirectToAction("Index", "Courses");
            }
        }
    }
}