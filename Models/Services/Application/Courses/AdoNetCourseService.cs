using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using Ganss.XSS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyCourse.Controllers;
using MyCourse.Models.Enums;
using MyCourse.Models.Exceptions;
using MyCourse.Models.Exceptions.Application;
using MyCourse.Models.Exceptions.Infrastructure;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.Options;
using MyCourse.Models.Services.Infrastructure;
using MyCourse.Models.ValueTypes;
using MyCourse.Models.ViewModels;
using MyCourse.Models.ViewModels.Courses;
using MyCourse.Models.ViewModels.Lessons;

namespace MyCourse.Models.Services.Application.Courses
{
    public class AdoNetCourseService : ICourseService
    {
        private readonly IDatabaseAccessor db;
        private readonly IOptionsMonitor<CoursesOptions> coursesOptions;
        private readonly ILogger<AdoNetCourseService> logger;
        private readonly IImagePersister imagePersister;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IEmailClient emailClient;
        private readonly IPaymentGateway paymentGateway;
        private readonly LinkGenerator linkGenerator;

        public AdoNetCourseService(IDatabaseAccessor db,
        IOptionsMonitor<CoursesOptions> coursesOptions,
        ILogger<AdoNetCourseService> logger,
        IImagePersister imagePersister, 
        IHttpContextAccessor httpContextAccessor, 
        IEmailClient emailClient,
        IPaymentGateway paymentGateway,
        LinkGenerator linkGenerator)
        {
            this.db = db;
            this.httpContextAccessor = httpContextAccessor;
            this.emailClient = emailClient;
            this.paymentGateway = paymentGateway;
            this.linkGenerator = linkGenerator;
            this.imagePersister = imagePersister;
            this.logger = logger;
            this.coursesOptions = coursesOptions;
            
        }

        public async Task<CourseDetailViewModel> CreateCourseAsync(CourseCreateInputModel inputModel)
        {
            string title = inputModel.Title;
            string author = "Mario Rossi"; //per il momento così poi si estrapola dal login

            try
            {
                int courseId = await db.QueryScalarAsync<int>($@"INSERT INTO Courses (Title, Author, ImagePath, Rating, CurrentPrice_Currency, CurrentPrice_Amount, FullPrice_Currency, FullPrice_Amount) VALUES ({title}, {author}, '/Courses/default.png', 0, 'EUR', 0, 'EUR', 0);
                                                SELECT last_insert_rowid()");
                CourseDetailViewModel course = await GetCourseAsync(courseId);
                return course;
            }
            catch (ConstraintViolationException exc)
            {
                throw new CourseTitleUnavailableException(inputModel.Title, exc);
            }
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
            logger.LogInformation("Course {id} requested", id);

            FormattableString query = $@"SELECT Id, Title, Description, ImagePath, Author, Rating, FullPrice_Amount, FullPrice_Currency, CurrentPrice_Amount, CurrentPrice_Currency FROM Courses WHERE Id={id}
            ; SELECT Id, Title, Description, Duration FROM Lessons WHERE CourseId={id} ORDER BY [Order], Id";

            DataSet dataSet = await db.QueryAsync(query);

            //Course
            DataTable courseTable = dataSet.Tables[0];
            if (courseTable.Rows.Count != 1)
            {
                logger.LogWarning("Course {id} not found", id);
                throw new CourseNotFoundException(id);
            }
            var courseRow = courseTable.Rows[0];
            var courseDetailViewModel = CourseDetailViewModel.FromDataRow(courseRow);

            //Course lessons
            var lessonDataTable = dataSet.Tables[1];

            foreach (DataRow lessonRow in lessonDataTable.Rows)
            {
                LessonViewModel lessonViewModel = LessonViewModel.FromDataRow(lessonRow);
                courseDetailViewModel.Lessons.Add(lessonViewModel);
            }
            return courseDetailViewModel;
        }



        public async Task<ListViewModel<CourseViewModel>> GetCoursesAsync(CourseListInputModel model)
        {
            //decidere cosa estrarre dal db
            string orderBy = model.OrderBy == "CurrentPrice" ? "CurrentPrice_Amount" : model.OrderBy;
            string direction = model.Ascending ? "ASC" : "DESC";

            FormattableString query = $@"SELECT Id, Title, ImagePath, Author, Rating, FullPrice_Amount, FullPrice_Currency, CurrentPrice_Amount, CurrentPrice_Currency FROM Courses WHERE title LIKE {"%" + model.Search + "%"} AND Status<>{nameof(CourseStatus.Deleted)} ORDER BY {(Sql)orderBy} {(Sql)direction} LIMIT {model.Limit} OFFSET {model.Offset};
            SELECT COUNT(*) FROM Courses WHERE title LIKE {"%" + model.Search + "%"} AND Status<>{nameof(CourseStatus.Deleted)}";
            DataSet dataSet = await db.QueryAsync(query);
            DataTable dataTable = dataSet.Tables[0];
            List<CourseViewModel> courseList = new();
            foreach (DataRow courseRow in dataTable.Rows)
            {
                CourseViewModel courseViewModel = CourseViewModel.FromDataRow(courseRow);
                courseList.Add(courseViewModel);
            }

            ListViewModel<CourseViewModel> result = new()
            {
                Results = courseList,
                TotalCount = Convert.ToInt32(dataSet.Tables[1].Rows[0][0])
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
            bool titleExists = await db.QueryScalarAsync<bool>($"SELECT COUNT(*) FROM Courses WHERE Title LIKE {title} AND id<>{id}");
            return !titleExists;
        }

        public async Task<CourseEditInputModel> GetCourseForEditingAsync(int id)
        {
            FormattableString query = $@"SELECT Id, Title, Description, ImagePath, Email, FullPrice_Amount, FullPrice_Currency, CurrentPrice_Amount, CurrentPrice_Currency, RowVersion FROM Courses WHERE Id={id}";
            DataSet dataSet = await db.QueryAsync(query);

            var courseTable = dataSet.Tables[0];
            if (courseTable.Rows.Count != 1)
            {
                logger.LogWarning("Course {id} not found", id);
                throw new CourseNotFoundException(id);
            }
            var courseRow = courseTable.Rows[0];
            var courseEditInputModel = CourseEditInputModel.FromDataRow(courseRow);
            return courseEditInputModel;
        }

        public async Task<CourseDetailViewModel> EditCourseAsync(CourseEditInputModel inputModel)
        {
            try
            {
                string imagePath = null;
                if (inputModel.Image != null)
                {
                    imagePath = await imagePersister.SaveCourseImageAsync(inputModel.Id, inputModel.Image);
                }
                int affectedRows = await db.CommandAsync($"UPDATE Courses SET ImagePath=COALESCE({imagePath}, ImagePath), Title={inputModel.Title}, Description={inputModel.Description}, Email={inputModel.Email}, CurrentPrice_Currency={inputModel.CurrentPrice.Currency}, CurrentPrice_Amount={inputModel.CurrentPrice.Amount}, FullPrice_Currency={inputModel.FullPrice.Currency}, FullPrice_Amount={inputModel.FullPrice.Amount} WHERE Id={inputModel.Id} AND RowVersion={inputModel.RowVersion}");
                if (affectedRows == 0)
                {
                    bool courseExist = await db.QueryScalarAsync<bool>($"SELECT COUNT(*) FROM Courses WHERE Id={inputModel.Id}");
                    if (courseExist)
                    {
                        throw new OptimisticConcurrencyException();
                    }
                    else
                    {
                        throw new CourseNotFoundException(inputModel.Id);
                    }

                }
            }
            catch (ConstraintViolationException exc)
            {
                throw new CourseTitleUnavailableException(inputModel.Title, exc);
            }
            catch (ImagePersistenceException exc)
            {
                throw new CourseImageInvalidException(inputModel.Id, exc);
            }

            CourseDetailViewModel course = await GetCourseAsync(inputModel.Id);
            return course;
        }

        public async Task DeleteCourseAsync(CourseDeleteInputModel inputModel)
        {
            int affectedRows = await db.CommandAsync($"UPDATE Courses SET Status={nameof(CourseStatus.Deleted)} WHERE Id={inputModel.Id} AND Status<>{nameof(CourseStatus.Deleted)}");

            if (affectedRows == 0)
            {
                throw new CourseNotFoundException(inputModel.Id);
            }


        }

        public async Task SendQuestionToCourseAuthorAsync(int id, string question)
        {
            //Recupero info del corso
            FormattableString query = $@"SELECT Title, Email FROM Courses WHERE Courses.Id={id}";
            DataSet dataSet = await db.QueryAsync(query); 

            if (dataSet.Tables[0].Rows.Count == 0)
            {
                logger.LogWarning("Course {id} not found", id);
                throw new CourseNotFoundException(id);
            }

            string courseTitle = Convert.ToString(dataSet.Tables[0].Rows[0]["Title"]);
            string courseEmail = Convert.ToString(dataSet.Tables[0].Rows[0]["Email"]);

            //Recupero info utente
            string userFullName;
            string userEmail;
            try
            {
                userFullName = httpContextAccessor.HttpContext.User.FindFirst("FullName").Value;
                userEmail = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Email).Value;
            }
            catch
            {
                throw new UserUnknownException();
            }

            //sanitizzo la domanda dell'utente
            question = new HtmlSanitizer(allowedTags: new String[0]).Sanitize(question);

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
            return db.QueryScalarAsync<string>($"SELECT AuthorId FROM Courses WHERE Id={courseId}");
        }

        public Task<int> GetCourseCountByAuthorIdAsync(string authorId)
        {
            return db.QueryScalarAsync<int>($"SELECT COUNT(*) FROM Courses WHERE AuthorId={authorId}");
        }

        public async Task SubscribeCourseAsync(CourseSubscribeInputModel inputModel)
        {
            try
            {
                await db.CommandAsync($"INSERT INTO Subscriptions (UserId, CourseId, PaymentDate, PaymentType, Paid_Currency, Paid_Amount, TransactionId) VALUES ({inputModel.UserId}, {inputModel.CourseId}, {inputModel.PaymentDate}, {inputModel.PaymentType}, {inputModel.Paid.Currency.ToString()}, {inputModel.Paid.Amount}, {inputModel.TransactionId})");
            }
            catch (ConstraintViolationException)
            {
                throw new CourseSubscriptionException(inputModel.CourseId);
            }
        }

        public Task<bool> IsCourseSubscribedAsync(int courseId, string userId)
        {
            return db.QueryScalarAsync<bool>($"SELECT COUNT(*) FROM Subscriptions WHERE CourseId={courseId} AND UserId={userId}");
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