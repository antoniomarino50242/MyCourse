using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyCourse.Models.Entities;
using MyCourse.Models.Entities.Services.Infrastructure;
using MyCourse.Models.Exceptions;
using MyCourse.Models.InputModels;
using MyCourse.Models.Options;
using MyCourse.Models.ViewModels;

namespace MyCourse.Models.Services.Application
{
    public class EfCoreCourseService : ICourseService
    {
        private readonly MyCourseDbContext dbContext;
        private readonly ILogger<EfCoreCourseService> logger;
        private readonly IOptionsMonitor<CoursesOptions> coursesOptions;

        public EfCoreCourseService(MyCourseDbContext dbContext, ILogger<EfCoreCourseService> logger, IOptionsMonitor<CoursesOptions> coursesOptions)
        {
            this.coursesOptions = coursesOptions;
            this.logger = logger;
            this.dbContext = dbContext;
        }

        public async Task<CourseDetailViewModel> GetCourseAsync(int id)
        {
            IQueryable<CourseDetailViewModel> queryLinq = dbContext.Courses
                .AsNoTracking()
                .Include(course => course.Lessons)
                .Where(course => course.Id == id)
                .Select(course => CourseDetailViewModel.FromEntity(course));

            //.FirstOrDefaultAsync(); //null se l'elenco è vuoto e non solleva mai un'eccezione
            //.SingleOrDefaultAsync();//tollera il caso in cui l'elenco è vuoto e restituisce il default, oppure se l'elenco contiene piu di uno solleva un'eccezione
            //.FirstAsync();//restituisc eil ptimo ma se l'elenco è vuoto solleva un eccezione 
            //.SingleAsync(); //restituisce il primo elemento se l'elenco ne contiene o 0 o più di uno solleva un'eccezione
            CourseDetailViewModel viewModel = await queryLinq.FirstOrDefaultAsync();
            if (viewModel == null)
            {
                logger.LogWarning("Course {id} not found", id);
                throw new CourseNotFoundException(id);
            }
            return viewModel;
        }

        public async Task<List<CourseViewModel>> GetCoursesAsync(CourseListInputModel model)
        {
            
            IQueryable<Course> baseQuery = dbContext.Courses;
            switch (model.OrderBy){
                case "Title": 
                    if (model.Ascending)
                    {
                        baseQuery = baseQuery.OrderBy(course => course.Title);
                    } else
                    {
                        baseQuery = baseQuery.OrderByDescending(course => course.Title);
                    }
                break;
                case "Rating" : 
                    if (model.Ascending)
                    {
                        baseQuery = baseQuery.OrderBy(course => course.Rating);
                    } else
                    {
                        baseQuery = baseQuery.OrderByDescending(course => course.Rating);
                    }
                break;
                case "CurrentPrice" : 
                    if (model.Ascending)
                    {
                        baseQuery = baseQuery.OrderBy(course => course.CurrentPrice.Amount);
                    } else
                    {
                        baseQuery = baseQuery.OrderByDescending(course => course.CurrentPrice.Amount);
                    }
                break;
            }

            IQueryable<CourseViewModel> queryLinq = baseQuery
                .Where(course => course.Title.Contains(model.Search))
                .Skip(model.Offset)
                .Take(model.Limit)
                .AsNoTracking()
                .Select(course => CourseViewModel.FromEntity(course));

            List<CourseViewModel> courses = await queryLinq.ToListAsync();
            return courses;
        }
    }
}