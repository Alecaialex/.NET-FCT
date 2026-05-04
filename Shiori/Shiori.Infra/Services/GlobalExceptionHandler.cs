using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shiori.Infra.Services
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        // Manejo general de excepciones en toda la API
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Añadimos los detalles del problema
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error interno del servidor",
                Detail = exception.Message
            };

            // Añadir el status code
            httpContext.Response.StatusCode = problemDetails.Status.Value;

            // Escribir la respuesta en formato JSON
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
