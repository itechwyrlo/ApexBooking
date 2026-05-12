using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetOrganizationDetail;

public sealed record GetOrganizationDetailQuery(string Slug) : IQuery<BaseResponse<OrganizationDetailDto>>;
