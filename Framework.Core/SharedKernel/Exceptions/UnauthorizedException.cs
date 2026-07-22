namespace ApexBooking.SharedKernel.Exceptions
{
    public class UnauthorizedException(string message, Exception? innerException = null)
        : BaseException(message, innerException);
}