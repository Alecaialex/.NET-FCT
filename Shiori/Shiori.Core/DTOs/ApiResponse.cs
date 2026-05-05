using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Core.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Operación realizada con éxito")
            => new ApiResponse<T> { Success = true, Data = data, Message = message };

        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
            => new ApiResponse<T> { Success = false, Message = message, Errors = errors };
    }
}
