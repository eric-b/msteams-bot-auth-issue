using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Hosting;

// See https://docs.microsoft.com/fr-fr/aspnet/core/web-api/handle-errors?view=aspnetcore-3.0#exception-handler

namespace TeamsChannelTester.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : ControllerBase
    {
        internal const string ErrorLocalDevRouteTemplate = "/api/error-local-development";

        private static readonly Exception GenericFallbackException = new Exception("Generic fallback exception");

        // It is recommended not to add [HttpGet] attribute here.
        [Route(ErrorLocalDevRouteTemplate)]
        public IActionResult ErrorLocalDevelopment([FromServices]IWebHostEnvironment webHostEnvironment)
        {
            if (!webHostEnvironment.IsDevelopment())
                throw new InvalidOperationException("This shouldn't be invoked in non-development environments.");

            Exception exception = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (exception == null)
            {
                // Can happen if we call directly this special endpoint.
                exception = GenericFallbackException;
                Response.StatusCode = 500;
            }

            return Problem(detail: $"{exception.Message}\r\n{exception.StackTrace}");
        }
    }
}
