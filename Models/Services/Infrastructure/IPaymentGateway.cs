using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.ValueTypes;

namespace MyCourse.Models.Services.Infrastructure
{
    public interface IPaymentGateway
    {
        Task<string> GetPaymentUrlAsync(CoursePayInputModel inputModel);

        Task<CourseSubscribeInputModel> CapturePaymentAsync(string token);
    }
}