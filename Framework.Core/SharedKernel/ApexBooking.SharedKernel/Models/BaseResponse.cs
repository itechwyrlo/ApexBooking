using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.SharedKernel.Models
{
    public class BaseResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public List<Error> Errors { get; set; }

        public static BaseResponse<T> Success(T data)
            => new BaseResponse<T> { IsSuccess = true, Data = data };

        public static BaseResponse<T> Failure(string message, string? code = null)
            => new BaseResponse<T>
            {
                IsSuccess = false,
                Errors = new List<Error> { new Error { Code = code, Message = message } }
            };
    }

    public record Error
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
    }
}