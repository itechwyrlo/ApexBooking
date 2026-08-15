namespace ApexBooking.Core.Domain.Services.Ticketing
{
    public interface IQrCodeGenerator
    {
        byte[] GeneratePng(string content);
    }
}
