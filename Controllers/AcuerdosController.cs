using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Services.Commercial;
using Microsoft.AspNetCore.Mvc;

namespace BudgetControl.Api.Controllers
{
    [ApiController]
    [Route("api/acuerdos")]
    public class AcuerdosController : ControllerBase
    {
        private readonly IComercialService _service;

        public AcuerdosController(IComercialService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAcuerdoRequest request)
        {
            try
            {
                var resultado = await _service.CreateAcuerdoAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = resultado.Id }, resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var resultado = await _service.GetAcuerdoDetalleAsync(id);
            if (resultado == null)
            {
                return NotFound();
            }
            return Ok(resultado);
        }

        [HttpGet("cliente/{clienteExternoId}")]
        public async Task<IActionResult> GetByCliente(string clienteExternoId)
        {
            return Ok(await _service.GetAcuerdosPorClienteAsync(clienteExternoId));
        }

        [HttpGet("obra/{obraExternaId}")]
        public async Task<IActionResult> GetByObra(string obraExternaId)
        {
            return Ok(await _service.GetAcuerdosPorObraAsync(obraExternaId));
        }

        [HttpPost("{id}/plan-pago")]
        public async Task<IActionResult> CreatePlanPago(int id, [FromBody] CreatePlanPagoRequest request)
        {
            try
            {
                var resultado = await _service.CrearPlanPagoAsync(id, request);
                return CreatedAtAction(nameof(GetById), new { id }, resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}/estado-comercial")]
        public async Task<IActionResult> GetEstadoComercial(int id)
        {
            try
            {
                return Ok(await _service.GetEstadoComercialAsync(id));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
