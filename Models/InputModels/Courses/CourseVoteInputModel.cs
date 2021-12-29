using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourse.Models.InputModels.Courses
{
    public class CourseVoteInputModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public int Vote { get; set; }
    }
}