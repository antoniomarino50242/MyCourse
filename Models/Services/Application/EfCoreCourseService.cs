using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
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

        public async Task<CourseDetailViewModel> CreateCourseAsync(CourseCreateInputModel inputModel)
        {
            string title = inputModel.Title;
            string author = "Mario Rossi";
            var course = new Course(title, author);
            dbContext.Add(course);
            try
            {     
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exc) when ((exc.InnerException as SqliteException)?.SqliteErrorCode == 19)
            {
                throw new CourseTitleUnavailableException(title, exc);
            }

            return CourseDetailViewModel.FromEntity(course);
        }

        public async Task<List<CourseViewModel>> GetBestRatingCoursesAsync()
        {
            CourseListInputModel inputModel = new CourseListInputModel(
                search : "",
                page : 1,
                orderBy : "Rating",
                ascending: false,
                limit: coursesOptions.CurrentValue.InHome,
                orderOptions: coursesOptions.CurrentValue.Order);

            ListViewModel<CourseViewModel> result = await GetCoursesAsync(inputModel);
            return result.Results;
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

        public async Task<ListViewModel<CourseViewModel>> GetCoursesAsync(CourseListInputModel model)
        {
            IQueryable<Course> baseQuery = dbContext.Courses;

            baseQuery = (model.OrderBy, model.Ascending) switch
            {
                ("Title", true) => baseQuery.OrderBy(course => course.Title),
                ("Title", false) => baseQuery.OrderByDescending(course => course.Title),               
                ("Rating", true) => baseQuery.OrderBy(course => course.Rating),
                ("Rating", false) => baseQuery.OrderByDescending(course => course.Rating),
                ("CurrentPrice", true) => baseQuery.OrderBy(course => course.CurrentPrice.Amount),
                ("CurrentPrice", false) => baseQuery.OrderByDescending(course => course.CurrentPrice.Amount),
                ("Id", true) => baseQuery.OrderBy(course => course.Id),
                ("Id", false) => baseQuery.OrderByDescending(course => course.Id),
                _ => baseQuery
            };
            
            IQueryable<Course> queryLinq = baseQuery
                .Where(course => course.Title.Contains(model.Search))
                .AsNoTracking();

            List<CourseViewModel> courses = await queryLinq                
                .Skip(model.Offset)
                .Take(model.Limit)
                .Select(course => CourseViewModel.FromEntity(course)) //Usando metodi statici come FromEntity, la query potrebbe essere inefficiente. Mantenere il mapping nella lambda oppure usare un extension method personalizzato
                .ToListAsync(); //La query al database viene inviata qui, quando manifestiamo l'intenzione di voler leggere i risultati

            int totalCount = await queryLinq.CountAsync();

            ListViewModel<CourseViewModel> result = new ListViewModel<CourseViewModel>
            {
                Results = courses,
                TotalCount = totalCount
            };

            return result;
        }

        public async Task<List<CourseViewModel>> GetMostRecentCoursesAsync()
        {
            CourseListInputModel inputModel = new CourseListInputModel(
                search : "",
                page : 1,
                orderBy : "Id",
                ascending: false,
                limit: coursesOptions.CurrentValue.InHome,
                orderOptions: coursesOptions.CurrentValue.Order);

            ListViewModel<CourseViewModel> result = await GetCoursesAsync(inputModel);
            return result.Results;
        }

        public async Task<bool> IsTitleAvailableAsync(string title)
        {
            bool titleExists = await dbContext.Courses.AnyAsync(course => EF.Functions.Like(course.Title, title));
            return !titleExists;
        }
    }
}