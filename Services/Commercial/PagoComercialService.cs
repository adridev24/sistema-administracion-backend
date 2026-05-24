using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Commercial;
using Microsoft.EntityFrameworkCore;

namespace BudgetControl.Api.Services.Commercial
{
    public class PagoComercialService : IPagoComercialService
    {
        private readonly AppDbContext _db;

        public PagoComercialService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PagoComercialResponse> RegistrarPagoAsync(CreatePagoComercialRequest request)
        {
            if (request.ImporteTotal <= 0)
            {
                throw new InvalidOperationException("El importe del pago debe ser mayor a cero.");
            }

            var acuerdo = await _db.AcuerdosComerciales.FindAsync(request.AcuerdoComercialId);
            if (acuerdo == null)
            {
                throw new InvalidOperationException("Acuerdo comercial no encontrado.");
            }

            if (acuerdo.Estado == AcuerdoEstado.Anulado)
            {
                throw new InvalidOperationException("No se puede registrar pago para un acuerdo anulado.");
            }

            var pago = new PagoComercial
            {
                ClienteExternoId = request.ClienteExternoId,
                ObraExternaId = request.ObraExternaId,
                AcuerdoComercialId = request.AcuerdoComercialId,
                FechaPago = request.FechaPago,
                ImporteTotal = request.ImporteTotal,
                MedioPago = request.MedioPago,
                Observaciones = request.Observaciones,
                Estado = PagoEstado.Registrado
            };

            _db.PagosComerciales.Add(pago);
            await _db.SaveChangesAsync();

            if (request.Aplicaciones.Any())
            {
                var updatedPago = await AplicarPagoInternoAsync(pago, request.Aplicaciones);
                return MapPago(updatedPago);
            }

            return MapPago(pago);
        }

        public async Task<PagoComercialResponse> AplicarPagoAsync(int pagoId, AplicarPagoRequest request)
        {
            var pago = await _db.PagosComerciales
                .Include(p => p.Aplicaciones)
                .FirstOrDefaultAsync(p => p.Id == pagoId);

            if (pago == null)
            {
                throw new InvalidOperationException("Pago comercial no encontrado.");
            }

            if (pago.Estado == PagoEstado.Anulado)
            {
                throw new InvalidOperationException("No se puede aplicar un pago anulado.");
            }

            var nuevasAplicaciones = request.Aplicaciones;
            if (!nuevasAplicaciones.Any())
            {
                throw new InvalidOperationException("Debe incluir al menos una aplicación de pago.");
            }

            var pagoActualizado = await AplicarPagoInternoAsync(pago, nuevasAplicaciones);
            return MapPago(pagoActualizado);
        }

        public async Task<IEnumerable<AplicacionPagoResponse>> GetAplicacionesPorCuotaAsync(int cuotaId)
        {
            var aplicaciones = await _db.AplicacionesPagoComerciales
                .Where(a => a.CuotaComercialId == cuotaId)
                .ToListAsync();

            return aplicaciones.Select(a => new AplicacionPagoResponse
            {
                Id = a.Id,
                PagoComercialId = a.PagoComercialId,
                CuotaComercialId = a.CuotaComercialId,
                ImporteAplicado = a.ImporteAplicado,
                FechaAplicacion = a.FechaAplicacion
            });
        }

        private async Task<PagoComercial> AplicarPagoInternoAsync(PagoComercial pago, List<AplicacionPagoRequest> aplicacionRequests)
        {
            var totalYaAplicado = pago.Aplicaciones.Sum(x => x.ImporteAplicado);
            var totalSolicitado = aplicacionRequests.Sum(r => r.ImporteAplicado);
            var saldoPago = pago.ImporteTotal - totalYaAplicado;

            if (totalSolicitado <= 0)
            {
                throw new InvalidOperationException("El importe aplicado debe ser mayor a cero.");
            }

            if (totalSolicitado > saldoPago)
            {
                throw new InvalidOperationException("No se puede aplicar más importe que el total disponible del pago.");
            }

            var cuotaIds = aplicacionRequests.Select(a => a.CuotaComercialId).Distinct().ToList();
            var cuotas = await _db.CuotasComerciales
                .Include(c => c.PlanPago)
                    .ThenInclude(p => p.AcuerdoComercial)
                .Where(c => cuotaIds.Contains(c.Id))
                .ToListAsync();

            if (cuotas.Count != cuotaIds.Count)
            {
                throw new InvalidOperationException("Algunas cuotas comerciales no se encontraron.");
            }

            foreach (var request in aplicacionRequests)
            {
                var cuota = cuotas.Single(c => c.Id == request.CuotaComercialId);
                if (cuota.PlanPago.AcuerdoComercialId != pago.AcuerdoComercialId)
                {
                    throw new InvalidOperationException("La cuota no pertenece al mismo acuerdo comercial del pago.");
                }

                if (request.ImporteAplicado > cuota.SaldoPendiente)
                {
                    throw new InvalidOperationException($"No se puede aplicar más importe que el saldo pendiente de la cuota {cuota.Id}.");
                }

                cuota.ImportePagado += request.ImporteAplicado;
                cuota.SaldoPendiente = Math.Max(cuota.SaldoPendiente - request.ImporteAplicado, 0);
                UpdateCuotaEstado(cuota);

                pago.Aplicaciones.Add(new AplicacionPagoComercial
                {
                    PagoComercialId = pago.Id,
                    CuotaComercialId = cuota.Id,
                    ImporteAplicado = request.ImporteAplicado,
                    FechaAplicacion = DateTime.UtcNow
                });
            }

            pago.Estado = pago.Aplicaciones.Sum(x => x.ImporteAplicado) > 0 ? PagoEstado.Aplicado : PagoEstado.Registrado;
            await _db.SaveChangesAsync();
            return pago;
        }

        private static void UpdateCuotaEstado(CuotaComercial cuota)
        {
            if (cuota.Estado == CuotaEstado.Anulada)
            {
                return;
            }

            if (cuota.SaldoPendiente <= 0)
            {
                cuota.Estado = CuotaEstado.Pagada;
                return;
            }

            if (cuota.FechaVencimiento < DateTime.UtcNow && cuota.ImportePagado == 0)
            {
                cuota.Estado = CuotaEstado.Vencida;
                return;
            }

            cuota.Estado = cuota.ImportePagado > 0 ? CuotaEstado.Parcial : CuotaEstado.Pendiente;
        }

        private static PagoComercialResponse MapPago(PagoComercial pago)
        {
            return new PagoComercialResponse
            {
                Id = pago.Id,
                ClienteExternoId = pago.ClienteExternoId,
                ObraExternaId = pago.ObraExternaId,
                AcuerdoComercialId = pago.AcuerdoComercialId,
                FechaPago = pago.FechaPago,
                ImporteTotal = pago.ImporteTotal,
                MedioPago = pago.MedioPago,
                Observaciones = pago.Observaciones,
                Estado = pago.Estado,
                Aplicaciones = pago.Aplicaciones.OrderBy(a => a.Id).Select(a => new AplicacionPagoResponse
                {
                    Id = a.Id,
                    PagoComercialId = a.PagoComercialId,
                    CuotaComercialId = a.CuotaComercialId,
                    ImporteAplicado = a.ImporteAplicado,
                    FechaAplicacion = a.FechaAplicacion
                }).ToList()
            };
        }
    }
}
