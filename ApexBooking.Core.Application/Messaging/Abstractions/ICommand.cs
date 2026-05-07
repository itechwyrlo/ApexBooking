using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace ApexBooking.Core.Application.Messaging.Abstractions
{
    public interface IBaseCommand { }

    public interface ICommand : IRequest, IBaseCommand { }

    public interface ICommand<TResponse> : IRequest<TResponse>, IBaseCommand { }

    // public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Unit>
    //     where TCommand : ICommand
    // { }

    public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    { }


    /// <summary>
    /// Defines a handler for a request with a void response.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being handled</typeparam>
    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Handles a request
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        Task Handle(TCommand request, CancellationToken cancellationToken);
    }
}