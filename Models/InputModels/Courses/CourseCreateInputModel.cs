using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MyCourse.Controllers;

namespace MyCourse.Models.InputModels.Courses
{
    public class CourseCreateInputModel
    {
        public string Title { get; set; }
    }
}