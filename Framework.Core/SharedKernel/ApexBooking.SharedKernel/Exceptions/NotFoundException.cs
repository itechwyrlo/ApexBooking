using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.SharedKernel.Exceptions
{
    public class NotFoundException(string message, Exception? innerException = null)
           : BaseException(message, innerException);
}