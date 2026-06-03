using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Route("api/comercial/reportes")]
    public class ReportesComercialesController : ControllerBase
    {
        private readonly IComercialService _service;

        public ReportesComercialesController(IComercialService service)
        {
            _service = service;
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var now = DateTime.UtcNow;
            var periodoDesde = DateTime.SpecifyKind(desde?.Date ?? new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);
            var periodoHasta = DateTime.SpecifyKind(hasta?.Date ?? periodoDesde.AddMonths(1).AddDays(-1), DateTimeKind.Utc);

            if (periodoHasta < periodoDesde)
            {
                return BadRequest(new { error = "La fecha hasta no puede ser anterior a la fecha desde." });
            }

            return Ok(await _service.GetReporteComercialResumenAsync(periodoDesde, periodoHasta));
        }
    }
}
