using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.SharedKernel.Exceptions
{
    /// <summary>
    /// Base exception for all domain-specific exceptions.
    /// </summary>
    public class BaseException(string? message = null, Exception? innerException = null)
        : Exception(message, innerException);
}