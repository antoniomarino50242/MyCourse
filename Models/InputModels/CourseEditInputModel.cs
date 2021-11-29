using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using MyCourse.Controllers;
using MyCourse.Models.Enums;
using MyCourse.Models.ValueTypes;

namespace MyCourse.Models.InputModels
{
    public class CourseEditInputModel : IValidatableObject
    {
        [Required]
        public int Id { get; set; }
        [Required(ErrorMessage ="Il titolo è obbligatorio."),
        MinLength(10, ErrorMessage = "Il titolo dev'essere almeno di {1} caratteri."),
        MaxLength(100, ErrorMessage = "Il titolo dev'essere di massimo {1} caratteri."),
        RegularExpression(@"^[\w\s\.]+$", ErrorMessage ="Titolo non valido."),
        Remote(action: nameof(CoursesController.IsTitleAvailable), controller: "Courses", ErrorMessage = "Il titolo è già stato utilizzato per un altro corso. Prova ad inserirne uno differente."),
        Display(Name = "Titolo")]
        public string Title { get; set; }
        [MinLength(10, ErrorMessage = "Il titolo dev'essere almeno di {1} caratteri."),
        MaxLength(4000, ErrorMessage = "Il titolo dev'essere di massimo {1} caratteri."),
        Display(Name = "Descrizione")]
        public string Description { get; set; }
        [Display(Name = "Immagine rappresentativa")]
        public string ImagePath { get; set; }
        [Required(ErrorMessage = "L'email di contatto è obbligatoria"),
         EmailAddress(ErrorMessage = "Devi inserire un indirizzo email"),
         Display(Name = "Email di contatto")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Il prezzo intero è obbligatorio"),
         Display(Name = "Prezzo intero")]
        public Money FullPrice { get; set; }
        [Required(ErrorMessage = "Il prezzo corrente è obbligatorio"),
         Display(Name = "Prezzo corrente")]
        public Money CurrentPrice { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FullPrice.Currency != CurrentPrice.Currency)
            {
                yield return new ValidationResult("Il prezzo intere deve avere la stessa valuta del prezzo corrente.", new[] {nameof(FullPrice)});
            }
            else if (FullPrice.Amount < CurrentPrice.Amount)
            {
                yield return new ValidationResult("Il prezzo intere non può essere inferiore al prezzo corrente.", new []{nameof(FullPrice)});
            }
        }

        public static CourseEditInputModel FromDataRow(DataRow courseRow)
        {
            var courseEditInputModel = new CourseEditInputModel
            {
                Title = Convert.ToString(courseRow["Title"]),
                Description = Convert.ToString(courseRow["Description"]),
                ImagePath = Convert.ToString(courseRow["ImagePath"]),
                Email = Convert.ToString(courseRow["Email"]),
                FullPrice = new Money(
                    Enum.Parse<Currency>(Convert.ToString(courseRow["FullPrice_Currency"])),
                    Convert.ToDecimal(courseRow["FullPrice_Amount"])
                ),
                CurrentPrice = new Money(
                    Enum.Parse<Currency>(Convert.ToString(courseRow["CurrentPrice_Currency"])),
                    Convert.ToDecimal(courseRow["CurrentPrice_Amount"])
                ),
                Id = Convert.ToInt32(courseRow["Id"])
            };
            return courseEditInputModel;
        }
    }
}