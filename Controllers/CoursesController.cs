using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyCourse.Models.Exceptions;
using MyCourse.Models.Exceptions.Application;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.Services.Application.Courses;
using MyCourse.Models.ViewModels;
using MyCourse.Models.ViewModels.Courses;

namespace MyCourse.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ICourseService courseService;
        public CoursesController(ICachedCourseService courseService)
        {
            this.courseService = courseService;
        }
        public async Task<IActionResult> Index(CourseListInputModel input)
        {
            ViewData["Title"] = "Catalogo dei corsi";
            ListViewModel<CourseViewModel> courses = await courseService.GetCoursesAsync(input);

            CourseListViewModel viewModel = new CourseListViewModel
            {
                Courses = courses,
                Input = input
            };
            return View(viewModel);
        }

        public async Task<IActionResult> IsTitleAvailable(string title, int id = 0)
        {
            bool result = await courseService.IsTitleAvailableAsync(title, id);
            return Json(result);
        }

        public async Task<IActionResult> Detail(int id)
        {
            CourseDetailViewModel viewModel = await courseService.GetCourseAsync(id);
            ViewData["Title"] = viewModel.Title;
            return View(viewModel);
        }

        public IActionResult Create()
        {
            //Viene mostrato il form all'utente
            ViewData["Title"] = "Nuovo corso";
            var inputModel = new CourseCreateInputModel();
            return View(inputModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync(CourseCreateInputModel inputModel) 
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //Viene coinvolto il servizio applicativo in modo che il corso venga creato
                    CourseDetailViewModel course = await courseService.CreateCourseAsync(inputModel);
                    TempData["ConfirmationMessage"] = "Corso creato con successo! Inserisci ora il resto delle informazioni.";
                    return RedirectToAction(nameof(Edit), new{ id = course.Id});
                }
                catch (CourseTitleUnavailableException)
                {
                    ModelState.AddModelError(nameof(CourseDetailViewModel.Title), "Il titolo inserito è già presente. Prova ad inserirne uno.");
                }
                catch (UserUnknownException) 
                {
                    ModelState.AddModelError(nameof(CourseDetailViewModel.Title), "Devi esser loggato per creare un corso. Registrati o accedi ad un account esistente!");
                }
            }
            ViewData["Title"] = "Nuovo corso";
            return View(inputModel);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Modifica corso";
            CourseEditInputModel inputModel = await courseService.GetCourseForEditingAsync(id);
            return View(inputModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CourseEditInputModel inputModel)
        {
            if (ModelState.IsValid)
            {
                //persisto i dati
                try
                {
                    CourseDetailViewModel course = await courseService.EditCourseAsync(inputModel);
                    TempData["ConfirmationMessage"] = "Dati modificati con successo!";
                    return RedirectToAction(nameof(Detail), new {id = inputModel.Id});
                }
                catch (CourseTitleUnavailableException)
                {
                    ModelState.AddModelError(nameof(CourseEditInputModel.Title), "Questo titolo già esiste");
                }
                catch (CourseImageInvalidException)
                {
                    ModelState.AddModelError(nameof(CourseEditInputModel.Image), "L'immagine selezionata non è valida! ");
                }
                catch(OptimisticConcurrencyException)
                {
                    ModelState.AddModelError("","Spiacenti il salvataggio non è andato a buon fine perchè nel frattempo un altro utente ha aggiornato il corso. Ti preghiamo di aggiornare la pagina e attuare le modifiche.");
                }
            }
            ViewData["Title"] = "Modifica corso";
            return View(inputModel);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(CourseDeleteInputModel inputModel)
        {
            await courseService.DeleteCourseAsync(inputModel);
            TempData["ConfirmationMessage"] = "Il corso è stato eliminato correttamente, ma potrebbe continuare a comparire negli elenchi per un breve periodo, finchè la cache non viene aggiornata.";
            return RedirectToAction(nameof(Index));
        }
    }
}