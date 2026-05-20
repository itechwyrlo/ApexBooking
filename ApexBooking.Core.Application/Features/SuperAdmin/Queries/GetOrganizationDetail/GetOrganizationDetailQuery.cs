using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetOrganizationDetail;

public sealed record GetOrganizationDetailQuery(string Slug) : IQuery<OrganizationDetailDto>;
