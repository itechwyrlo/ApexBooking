namespace ApexBooking.SharedKernel.Exceptions
{
    public class NotFoundException(string message, Exception? innerException = null)
           : BaseException(message, innerException);
}