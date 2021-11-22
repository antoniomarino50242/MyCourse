using Microsoft.AspNetCore.Mvc;
using MyCourse.Models.ViewModels;

namespace MyCourse.Customization.ViewComponents
{
    public class PaginationBarViewComponent : ViewComponent
    {    
        //public IViewComponentResult Invoke(CourseListViewModel model){
        
        public IViewComponentResult Invoke(IPaginationInfo model){
            
            //Il numero della pagina corrente
            //Il numero di risultati totali
            //Il numero di risultati per pagina
            //Search, order-by e ascending
            return View(model);
        }
    }
}