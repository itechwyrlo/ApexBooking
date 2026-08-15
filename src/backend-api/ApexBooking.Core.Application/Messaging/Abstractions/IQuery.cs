using MediatR;

namespace ApexBooking.Core.Application.Messaging.Abstractions
{
    public interface IBaseQuery { }

    public interface IQuery<TResponse> : IRequest<TResponse>, IBaseQuery { }

    public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse> { }
}