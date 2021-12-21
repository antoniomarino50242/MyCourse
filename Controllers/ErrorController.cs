using System;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyCourse.Models.Exceptions;
using MyCourse.Models.Exceptions.Application;

namespace MyCourse.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult Index() 
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            switch (feature.Error)
            {
                case CourseNotFoundException exc:
                    ViewData["Title"] = "Corso non trovato";
                    Response.StatusCode = 404;
                    return View("CourseNotFound");
                case SendException exc:
                    ViewData["Title"] = "Non è stato possibile inviare il messaggio, riprova più tardi.";
                    Response.StatusCode = 500;
                    return View();
                case UserUnknownException exc:
                    ViewData["Title"] = "Utente sconosciuto";
                    return View();
                default:
                     ViewData["Title"] = "Errore";
                     return View();
            }
        }
    }
}