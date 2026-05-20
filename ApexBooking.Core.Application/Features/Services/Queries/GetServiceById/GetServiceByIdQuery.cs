using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Services.Queries.GetServiceById;

public sealed record GetServiceByIdQuery(Guid ServiceId) : IQuery<ServiceDto>;