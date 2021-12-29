using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Ganss.XSS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyCourse.Controllers;
using MyCourse.Models.Entities;
using MyCourse.Models.Exceptions;
using MyCourse.Models.Exceptions.Application;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.Options;
using MyCourse.Models.Services.Infrastructure;
using MyCourse.Models.ValueTypes;
using MyCourse.Models.ViewModels;
using MyCourse.Models.ViewModels.Courses;

namespace MyCourse.Models.Services.Application.Courses
{
    public class EfCoreCourseService : ICourseService
    {
        private readonly MyCourseDbContext dbContext;
        private readonly ILogger<EfCoreCourseService> logger;
        private readonly IOptionsMonitor<CoursesOptions> coursesOptions;
        private readonly IImagePersister imagePersister;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IEmailClient emailClient;
        private readonly LinkGenerator linkGenerator;
        private readonly IPaymentGateway paymentGateway;

        public EfCoreCourseService(IHttpContextAccessor contextAccessor,
         MyCourseDbContext dbContext, 
         ILogger<EfCoreCourseService> logger, 
         IOptionsMonitor<CoursesOptions> coursesOptions, 
         IImagePersister imagePersister, 
         IHttpContextAccessor httpContextAccessor, 
         IEmailClient emailClient,
         LinkGenerator linkGenerator,
         IPaymentGateway paymentGateway)
        {
            this.emailClient = emailClient;
            this.linkGenerator = linkGenerator;
            this.paymentGateway = paymentGateway;
            this.httpContextAccessor = httpContextAccessor;
            this.contextAccessor = contextAccessor;
            this.imagePersister = imagePersister;
            this.coursesOptions = coursesOptions;
            this.logger = logger;
            this.dbContext = dbContext;
        }

        public async Task<CourseDetailViewModel> CreateCourseAsync(CourseCreateInputModel inputModel)
        {
            string title = inputModel.Title;
            string author;
            string authorId;
            try
            {
                author = contextAccessor.HttpContext.User.FindFirst("FullName").Value;
                authorId = contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
            catch (NullReferenceException)
            {
                throw new UserUnknownException();
            }

            var course = new Course(title, author, authorId);
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
                search: "",
                page: 1,
                orderBy: "Rating",
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
                search: "",
                page: 1,
                orderBy: "Id",
                ascending: false,
                limit: coursesOptions.CurrentValue.InHome,
                orderOptions: coursesOptions.CurrentValue.Order);

            ListViewModel<CourseViewModel> result = await GetCoursesAsync(inputModel);
            return result.Results;
        }

        public async Task<bool> IsTitleAvailableAsync(string title, int id)
        {
            bool titleExists = await dbContext.Courses.AnyAsync(course => EF.Functions.Like(course.Title, title) && course.Id != id);
            return !titleExists;
        }

        public async Task<CourseEditInputModel> GetCourseForEditingAsync(int id)
        {
            IQueryable<CourseEditInputModel> queryLinq = dbContext.Courses
                .AsNoTracking()
                .Where(course => course.Id == id)
                .Select(course => CourseEditInputModel.FromEntity(course));

            CourseEditInputModel viewModel = await queryLinq.FirstOrDefaultAsync();

            if (viewModel == null)
            {
                logger.LogWarning("Course {id} not found", id);
                throw new CourseNotFoundException(id);
            }

            return viewModel;
        }

        public async Task<CourseDetailViewModel> EditCourseAsync(CourseEditInputModel inputModel)
        {
            Course course = await dbContext.Courses.FindAsync(inputModel.Id);

            if (course == null)
            {
                throw new CourseNotFoundException(inputModel.Id);
            }

            course.ChangeTitle(inputModel.Title);
            course.ChangePrice(inputModel.FullPrice, inputModel.CurrentPrice);
            course.ChangeDescription(inputModel.Description);
            course.ChangeEmail(inputModel.Email);

            dbContext.Entry(course).Property(course => course.RowVersion).OriginalValue = inputModel.RowVersion;

            if (inputModel.Image != null)
            {
                try
                {
                    string imagePath = await imagePersister.SaveCourseImageAsync(inputModel.Id, inputModel.Image);
                    course.ChangeImagePath(imagePath);
                }
                catch (Exception exc)
                {
                    throw new CourseImageInvalidException(inputModel.Id, exc);
                }
            }

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new OptimisticConcurrencyException();
            }
            catch (DbUpdateException exc) when ((exc.InnerException as SqliteException)?.SqliteErrorCode == 19)
            {
                throw new CourseTitleUnavailableException(inputModel.Title, exc);
            }
            return CourseDetailViewModel.FromEntity(course);
        }
        public async Task DeleteCourseAsync(CourseDeleteInputModel inputModel)
        {
            Course course = await dbContext.Courses.FindAsync(inputModel.Id);

            if (course == null)
            {
                throw new CourseNotFoundException(inputModel.Id);
            }

            course.changeStatus(Enums.CourseStatus.Deleted);
            await dbContext.SaveChangesAsync();
        }

        public async Task SendQuestionToCourseAuthorAsync(int courseId, string question)
        {
            //recupero info del corso
            Course course = await dbContext.Courses.FindAsync(courseId);

            if (course == null)
            {
                throw new CourseNotFoundException(courseId);
            }

            string courseTitle = course.Title;
            string courseEmail = course.Email;

            //recupero utente
            string userFullName;
            string userEmail;

            try
            {
                userFullName = httpContextAccessor.HttpContext.User.FindFirst("FullName").Value;
                userEmail = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Email).Value;
            }
            catch (NullReferenceException)
            {
                throw new UserUnknownException();
            }


            //sanitizzo la domanda dell'utente
            question = new HtmlSanitizer(allowedTags: new string[0]).Sanitize(question);

            //Compongo il testo della email
            string subject = $@"Domanda per il corso ""{courseTitle}""";
            string message = $@"<p> L'utente {userFullName} (<a href=""{userEmail}"">{userEmail}</a>)
                                ti ha inviato la seguente domanda: </p>
                                <p>{question}</p>";

            //invio domanda
            try
            {
                await emailClient.SendEmailAsync(courseEmail, userEmail, subject, message);
            }
            catch (System.Exception)
            {
                throw new SendException();
            }
        }

        public Task<string> GetCourseAuthorIdAsync(int courseId)
        {
            return dbContext.Courses
                            .Where(course => course.Id == courseId)
                            .Select(course => course.AuthorId)
                            .FirstOrDefaultAsync();
        }

        public Task<int> GetCourseCountByAuthorIdAsync(string authorId)
        {
            return dbContext.Courses
                            .Where(course => course.AuthorId == authorId)
                            .CountAsync();
        }

        public async Task SubscribeCourseAsync(CourseSubscribeInputModel inputModel)
        {
            Subscription subscription = new(inputModel.UserId, inputModel.CourseId)
            {
                PaymentDate = inputModel.PaymentDate,
                PaymentType = inputModel.PaymentType,
                Paid = inputModel.Paid,
                TransactionId = inputModel.TransactionId
            };

            dbContext.Subscriptions.Add(subscription);
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new CourseSubscriptionException(inputModel.CourseId);
            }
        }

        public Task<bool> IsCourseSubscribedAsync(int courseId, string userId)
        {
            return dbContext.Subscriptions.Where(subscription => subscription.CourseId == courseId && subscription.UserId == userId).AnyAsync();
        }

        public Task<CourseSubscribeInputModel> CapturePaymentAsync(int id, string token)
        {
            return paymentGateway.CapturePaymentAsync(token);
        }

        public async Task<string> GetPaymentUrlAsync(int courseId)
        {
            CourseDetailViewModel viewModel = await GetCourseAsync(courseId);
            CoursePayInputModel inputModel = new()
            {
                CourseId = courseId,
                UserId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                Description = viewModel.Title,
                Price = viewModel.CurrentPrice,
                ReturnUrl = linkGenerator.GetUriByAction(httpContextAccessor.HttpContext,
                                        action: nameof(CoursesController.Subscribe),
                                        controller: "Courses",
                                        values: new {id = courseId}),
                CancelUrl = linkGenerator.GetUriByAction(httpContextAccessor.HttpContext,
                                        action: nameof(CoursesController.Detail),
                                        controller: "Courses",
                                        values: new {id = courseId})
            };
            return await paymentGateway.GetPaymentUrlAsync(inputModel);
        }
    }
}