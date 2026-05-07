using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Resources.Queries.GetResourceExceptions
{
    internal sealed class GetResourceExceptionsQueryHandler
     : IQueryHandler<GetResourceExceptionsQuery, BaseResponse<List<ResourceAvailabilityExceptionDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetResourceExceptionsQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<List<ResourceAvailabilityExceptionDto>>> Handle(
            GetResourceExceptionsQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var resourceId = new ResourceId(query.ResourceId);

            var resource = await _unitOfWork.ResourceRepository
                .FindByIdWithAvailabilityAsync(resourceId, cancellationToken)
                .ConfigureAwait(false);

            if (resource is null || resource.TenantId != tenantId)
                return BaseResponse<List<ResourceAvailabilityExceptionDto>>.Failure("Resource not found.");

            var dtos = resource.AvailabilityExceptions.Select(e => new ResourceAvailabilityExceptionDto
            {
                Id = e.ResourceAvailabilityExceptionId.Value,
                ExceptionDate = e.ExceptionDate,
                ExceptionType = e.ExceptionType.ToString(),
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Note = e.Note
            }).ToList();

            return BaseResponse<List<ResourceAvailabilityExceptionDto>>.Success(dtos);
        }
    }
}