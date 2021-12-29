using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourse.Models.Exceptions.Infrastructure
{
    public class PaymentGatewayException : Exception
    {
        public PaymentGatewayException(Exception innerException) : base($"Payment gataway threw an exception: ", innerException)
        {
        }
    }
}