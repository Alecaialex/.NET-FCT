using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Utilidades
{
    public class FiltroValidacionLibro : IAsyncActionFilter
    {
        private readonly ApplicationDbContext dbContext;

        public FiltroValidacionLibro(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ActionArguments.TryGetValue("libroCreacionDTO", out var value) || 
                value is not LibroCreacionDTO libroCreacionDTO)
            {
                context.ModelState.AddModelError(string.Empty, "El modelo enviado no es válido");
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            if (libroCreacionDTO.AutoresIds == null || libroCreacionDTO.AutoresIds.Count == 0)
            {
                context.ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds), "No se puede crear un libro sin autores");
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            var autoresIdsExisten = await dbContext.Autores
                .Where(x => libroCreacionDTO.AutoresIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (autoresIdsExisten.Count != libroCreacionDTO.AutoresIds.Count)
            {
                var autoresNoExisten = libroCreacionDTO.AutoresIds.Except(autoresIdsExisten);
                var mensaje = $"No existen los autores con los siguientes ids: {string.Join(", ", autoresNoExisten)}";

                context.ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds), mensaje);
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            await next();
        }
    }
}
