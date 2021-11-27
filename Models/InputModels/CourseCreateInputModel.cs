using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MyCourse.Controllers;

namespace MyCourse.Models.InputModels
{
    public class CourseCreateInputModel
    {
        [Required(ErrorMessage = "Il titolo è obbligatorio"),
        MinLength(10, ErrorMessage = "Il titolo deve essere di almeno {1} caratteri"),
        MaxLength(100, ErrorMessage = "Il titolo deve essere massimo di {1} caratteri"), 
        RegularExpression(@"^[\w\s\.]+$", ErrorMessage = "Titolo non valido. Il titolo può esser composto solo da numeri, lettere, spazi e punti."),
        Remote(action: nameof(CoursesController.IsTitleAvailable), controller: "Courses", ErrorMessage = "Il titolo è già presente. Prova ad inserirne uno differente.")]
        public string Title { get; set; }
    }
}