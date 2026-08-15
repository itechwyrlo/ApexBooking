namespace ApexBooking.Core.Application.Dtos.Response
{
    public record TenantRevenueDto(decimal OnlineAmount, decimal PayInVisitAmount, decimal Total, string CurrencyCode);
}
