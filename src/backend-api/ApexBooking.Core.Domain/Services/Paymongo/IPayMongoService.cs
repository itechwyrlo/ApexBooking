using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Domain.Services.Paymongo
{
    public interface IPayMongoService
{
    Task<PayMongoSourceResult> CreatePaymentSourceAsync(
            string tenantSecretKey,
            Guid bookingId,
            Guid branchId,
            decimal amountPhp,
            string description,
            CancellationToken cancellationToken);
    }
}