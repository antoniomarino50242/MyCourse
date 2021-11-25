using System.ComponentModel.DataAnnotations;

namespace MyCourse.Models.InputModels
{
    public class CourseCreateInputModel
    {
        [Required(ErrorMessage = "Il titolo è obbligatorio"),
        MinLength(10, ErrorMessage = "Il titolo deve essere di almeno {1} caratteri"),
        MaxLength(100, ErrorMessage = "Il titolo deve essere massimo di {1} caratteri"), 
        RegularExpression(@"^[\w\s\.]+$", ErrorMessage = "Titolo non valido. Il titolo può esser composto solo da numeri, lettere, spazi e punti.")]
        public string Title { get; set; }
    }
}