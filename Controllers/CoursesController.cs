using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyCourse.Models.Enums;
using MyCourse.Models.Exceptions;
using MyCourse.Models.Exceptions.Application;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.Options;
using MyCourse.Models.Services.Application.Courses;
using MyCourse.Models.Services.Infrastructure;
using MyCourse.Models.ValueTypes;
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

        [AllowAnonymous]
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

        [AllowAnonymous]
        public async Task<IActionResult> Detail(int id)
        {
            CourseDetailViewModel viewModel = await courseService.GetCourseAsync(id);
            ViewData["Title"] = viewModel.Title;
            return View(viewModel);
        }

        [Authorize(Roles = nameof(Role.Teacher))]
        public IActionResult Create()
        {
            //Viene mostrato il form all'utente
            ViewData["Title"] = "Nuovo corso";
            var inputModel = new CourseCreateInputModel();
            return View(inputModel);
        }

        [HttpPost]
        [Authorize(Roles = nameof(Role.Teacher))]
        public async Task<IActionResult> CreateAsync(CourseCreateInputModel inputModel, [FromServices] IAuthorizationService authorizationService, [FromServices] IEmailClient emailClient, [FromServices] IOptionsMonitor<UsersOptions> usersOptions)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //Viene coinvolto il servizio applicativo in modo che il corso venga creato
                    CourseDetailViewModel course = await courseService.CreateCourseAsync(inputModel);

                    AuthorizationResult result = await authorizationService.AuthorizeAsync(User, nameof(Policy.CourseLimit));
                    if (!result.Succeeded)
                    {
                        await emailClient.SendEmailAsync(usersOptions.CurrentValue.NotificationEmailRecipient, "Avviso superamento soglia", $"Il docente {User.Identity.Name} ha molti corsi: verifica che riesca a gestirli tutti.");
                    }

                    TempData["ConfirmationMessage"] = "Corso creato con successo! Inserisci ora il resto delle informazioni.";
                    return RedirectToAction(nameof(Edit), new { id = course.Id });
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

        [Authorize(Policy = nameof(Policy.CourseAuthor))]
        [Authorize(Roles = nameof(Role.Teacher))]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Modifica corso";
            CourseEditInputModel inputModel = await courseService.GetCourseForEditingAsync(id);
            return View(inputModel);
        }

        [HttpPost]
        [Authorize(Policy = nameof(Policy.CourseAuthor))]
        [Authorize(Roles = nameof(Role.Teacher))]
        public async Task<IActionResult> Edit(CourseEditInputModel inputModel)
        {
            if (ModelState.IsValid)
            {
                //persisto i dati
                try
                {
                    CourseDetailViewModel course = await courseService.EditCourseAsync(inputModel);
                    TempData["ConfirmationMessage"] = "Dati modificati con successo!";
                    return RedirectToAction(nameof(Detail), new { id = inputModel.Id });
                }
                catch (CourseTitleUnavailableException)
                {
                    ModelState.AddModelError(nameof(CourseEditInputModel.Title), "Questo titolo già esiste");
                }
                catch (CourseImageInvalidException)
                {
                    ModelState.AddModelError(nameof(CourseEditInputModel.Image), "L'immagine selezionata non è valida! ");
                }
                catch (OptimisticConcurrencyException)
                {
                    ModelState.AddModelError("", "Spiacenti il salvataggio non è andato a buon fine perchè nel frattempo un altro utente ha aggiornato il corso. Ti preghiamo di aggiornare la pagina e attuare le modifiche.");
                }
            }
            ViewData["Title"] = "Modifica corso";
            return View(inputModel);
        }

        [HttpPost]
        [Authorize(Policy = nameof(Policy.CourseAuthor))]
        [Authorize(Roles = nameof(Role.Teacher))]
        public async Task<IActionResult> Delete(CourseDeleteInputModel inputModel)
        {
            await courseService.DeleteCourseAsync(inputModel);
            TempData["ConfirmationMessage"] = "Il corso è stato eliminato correttamente, ma potrebbe continuare a comparire negli elenchi per un breve periodo, finchè la cache non viene aggiornata.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Subscribe(int id, string token)
        {
            CourseSubscribeInputModel inputModel = await courseService.CapturePaymentAsync(id, token);
            await courseService.SubscribeCourseAsync(inputModel);
            TempData["ConfirmationMessage"] = "Grazie per esserti iscritto, guarda subito la prima lezione!";
            return RedirectToAction(nameof(Detail), new { id = id });
        }

        public async Task<IActionResult> Pay(int id)
        {
            string paymentUrl = await courseService.GetPaymentUrlAsync(id);
            return Redirect(paymentUrl);
        }

        [Authorize(Policy = nameof(Policy.CourseSubscriber))]
        public async Task<IActionResult> VoteAsync(int id) 
        {
            CourseVoteInputModel inputModel = new()
            {
                Id = id,
                Vote = await courseService.GetCourseVoteAsync(id) ?? 0
            };
            return View(inputModel);
        }

        [Authorize(Policy = nameof(Policy.CourseSubscriber))]
        [HttpPost]
        public async Task<IActionResult> VoteAsync(CourseVoteInputModel inputModel) 
        {
            await courseService.VoteCourseAsync(inputModel);
            TempData["ConfirmationMessage"] = "Grazie per aver votato!";
            return RedirectToAction(nameof(Detail), new {id = inputModel.Id});
        }


    }
}