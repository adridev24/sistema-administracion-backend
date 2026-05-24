using BudgetControl.Api.Data;
using BudgetControl.Api.DTOs.Commercial;
using BudgetControl.Api.Models.Commercial;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BudgetControl.Api.Services.Commercial
{
    public class ComercialService : IComercialService
    {
        private readonly AppDbContext _db;

        public ComercialService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<AcuerdoResponse> CreateAcuerdoAsync(CreateAcuerdoRequest request)
        {
            if (request.MontoTotal <= 0)
            {
                throw new InvalidOperationException("El monto total del acuerdo debe ser mayor a cero.");
            }

            var acuerdo = new AcuerdoComercial
            {
                ClienteExternoId = request.ClienteExternoId,
                ObraExternaId = request.ObraExternaId,
                NumeroAcuerdo = request.NumeroAcuerdo,
                FechaAcuerdo = request.FechaAcuerdo,
                Descripcion = request.Descripcion,
                MontoTotal = request.MontoTotal,
                Estado = request.Estado,
                ViaOperacion = request.ViaOperacion,
                Observaciones = request.Observaciones,
                FechaAlta = DateTime.UtcNow,
                UsuarioAlta = request.UsuarioAlta
            };

            _db.Add(acuerdo);
            await _db.SaveChangesAsync();

            return MapAcuerdo(acuerdo);
        }

        public async Task<AcuerdoDetalleResponse?> GetAcuerdoDetalleAsync(int id)
        {
            var acuerdo = await _db.AcuerdosComerciales
                .Include(a => a.PlanPago)!
                    .ThenInclude(p => p.Cuotas)
                .Include(a => a.Pagos)
                    .ThenInclude(p => p.Aplicaciones)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (acuerdo == null)
            {
                return null;
            }

            var detalle = new AcuerdoDetalleResponse
            {
                Id = acuerdo.Id,
                ClienteExternoId = acuerdo.ClienteExternoId,
                ObraExternaId = acuerdo.ObraExternaId,
                NumeroAcuerdo = acuerdo.NumeroAcuerdo,
                FechaAcuerdo = acuerdo.FechaAcuerdo,
                Descripcion = acuerdo.Descripcion,
                MontoTotal = acuerdo.MontoTotal,
                Estado = acuerdo.Estado,
                ViaOperacion = acuerdo.ViaOperacion,
                Observaciones = acuerdo.Observaciones,
                FechaAlta = acuerdo.FechaAlta,
                UsuarioAlta = acuerdo.UsuarioAlta,
                PlanPago = acuerdo.PlanPago == null ? null : MapPlanPago(acuerdo.PlanPago)
            };

            foreach (var pago in acuerdo.Pagos)
            {
                detalle.Pagos.Add(MapPago(pago));
            }

            return detalle;
        }

        public async Task<IEnumerable<AcuerdoResponse>> GetAcuerdosPorClienteAsync(string clienteExternoId)
        {
            return await _db.AcuerdosComerciales
                .Where(a => a.ClienteExternoId == clienteExternoId)
                .OrderByDescending(a => a.FechaAlta)
                .Select(MapAcuerdoExpression)
                .ToListAsync();
        }

        public async Task<IEnumerable<AcuerdoResponse>> GetAcuerdosPorObraAsync(string obraExternaId)
        {
            return await _db.AcuerdosComerciales
                .Where(a => a.ObraExternaId == obraExternaId)
                .OrderByDescending(a => a.FechaAlta)
                .Select(MapAcuerdoExpression)
                .ToListAsync();
        }

        public async Task<PlanPagoResponse> CrearPlanPagoAsync(int acuerdoId, CreatePlanPagoRequest request)
        {
            var acuerdo = await _db.AcuerdosComerciales
                .Include(a => a.PlanPago)
                .FirstOrDefaultAsync(a => a.Id == acuerdoId);

            if (acuerdo == null)
            {
                throw new InvalidOperationException("Acuerdo comercial no encontrado.");
            }

            if (acuerdo.Estado == AcuerdoEstado.Anulado || acuerdo.Estado == AcuerdoEstado.Finalizado)
            {
                throw new InvalidOperationException("No se puede crear un plan de pago para un acuerdo finalizado o anulado.");
            }

            if (acuerdo.PlanPago != null)
            {
                throw new InvalidOperationException("El acuerdo ya tiene un plan de pago asociado.");
            }

            if (request.CantidadCuotas <= 0)
            {
                throw new InvalidOperationException("La cantidad de cuotas debe ser mayor a cero.");
            }

            if (request.TieneAnticipo)
            {
                if (request.MontoAnticipo <= 0)
                {
                    throw new InvalidOperationException("El monto de anticipo debe ser mayor a cero cuando hay anticipo.");
                }

                if (request.MontoAnticipo >= acuerdo.MontoTotal)
                {
                    throw new InvalidOperationException("El anticipo debe ser menor al monto total del acuerdo.");
                }
            }
            else if (request.MontoAnticipo != 0)
            {
                throw new InvalidOperationException("No puede enviar un monto de anticipo cuando no tiene anticipo.");
            }

            var totalRemanente = acuerdo.MontoTotal - request.MontoAnticipo;
            if (totalRemanente <= 0)
            {
                throw new InvalidOperationException("El monto total de cuotas debe ser mayor a cero.");
            }

            var plan = new PlanPago
            {
                AcuerdoComercialId = acuerdoId,
                TieneAnticipo = request.TieneAnticipo,
                MontoAnticipo = request.MontoAnticipo,
                CantidadCuotas = request.CantidadCuotas,
                FechaPrimerVencimiento = request.FechaPrimerVencimiento,
                Periodicidad = request.Periodicidad,
                Observaciones = request.Observaciones
            };

            if (request.TieneAnticipo)
            {
                plan.Cuotas.Add(new CuotaComercial
                {
                    NumeroCuota = 0,
                    TipoCuota = TipoCuota.Anticipo,
                    FechaVencimiento = request.FechaPrimerVencimiento,
                    ImporteOriginal = request.MontoAnticipo,
                    ImportePagado = 0,
                    SaldoPendiente = request.MontoAnticipo,
                    Estado = CuotaEstado.Pendiente
                });
            }

            var cuotas = BuildCuotas(request, totalRemanente);
            foreach (var cuota in cuotas)
            {
                plan.Cuotas.Add(cuota);
            }

            _db.PlanesPago.Add(plan);
            await _db.SaveChangesAsync();

            return MapPlanPago(plan);
        }

        public async Task<EstadoComercialResponse> GetEstadoComercialAsync(int acuerdoId)
        {
            var acuerdo = await _db.AcuerdosComerciales
                .Include(a => a.Pagos)
                    .ThenInclude(p => p.Aplicaciones)
                .FirstOrDefaultAsync(a => a.Id == acuerdoId);

            if (acuerdo == null)
            {
                throw new InvalidOperationException("Acuerdo comercial no encontrado.");
            }

            var totalPagado = acuerdo.Pagos.SelectMany(p => p.Aplicaciones).Sum(x => x.ImporteAplicado);
            return new EstadoComercialResponse
            {
                AcuerdoComercialId = acuerdo.Id,
                TotalPrometido = acuerdo.MontoTotal,
                TotalPagado = totalPagado,
                SaldoRestante = Math.Max(acuerdo.MontoTotal - totalPagado, 0)
            };
        }

        public async Task<SaldoComercialResponse> GetSaldoComercialClienteAsync(string clienteExternoId)
        {
            var acuerdos = await _db.AcuerdosComerciales
                .Where(a => a.ClienteExternoId == clienteExternoId && a.Estado != AcuerdoEstado.Anulado)
                .Include(a => a.Pagos)
                    .ThenInclude(p => p.Aplicaciones)
                .ToListAsync();

            return MapSaldo(clienteExternoId, acuerdos);
        }

        public async Task<SaldoComercialResponse> GetSaldoComercialObraAsync(string obraExternaId)
        {
            var acuerdos = await _db.AcuerdosComerciales
                .Where(a => a.ObraExternaId == obraExternaId && a.Estado != AcuerdoEstado.Anulado)
                .Include(a => a.Pagos)
                    .ThenInclude(p => p.Aplicaciones)
                .ToListAsync();

            return MapSaldo(obraExternaId, acuerdos);
        }

        public async Task<IEnumerable<CuotaResponse>> GetCuotasVencidasAsync()
        {
            var cuotas = await _db.CuotasComerciales
                .Where(c => c.SaldoPendiente > 0 && c.FechaVencimiento < DateTime.UtcNow && c.Estado != CuotaEstado.Anulada)
                .OrderBy(c => c.FechaVencimiento)
                .ToListAsync();

            cuotas.ForEach(UpdateCuotaEstado);
            await _db.SaveChangesAsync();
            return cuotas.Select(MapCuota).ToList();
        }

        public async Task<IEnumerable<CuotaResponse>> GetCuotasPendientesAsync()
        {
            var cuotas = await _db.CuotasComerciales
                .Where(c => c.SaldoPendiente > 0 && c.Estado != CuotaEstado.Pagada && c.Estado != CuotaEstado.Anulada)
                .OrderBy(c => c.FechaVencimiento)
                .ToListAsync();

            cuotas.ForEach(UpdateCuotaEstado);
            await _db.SaveChangesAsync();
            return cuotas.Select(MapCuota).ToList();
        }

        private static PlanPagoResponse MapPlanPago(PlanPago plan)
        {
            return new PlanPagoResponse
            {
                Id = plan.Id,
                AcuerdoComercialId = plan.AcuerdoComercialId,
                TieneAnticipo = plan.TieneAnticipo,
                MontoAnticipo = plan.MontoAnticipo,
                CantidadCuotas = plan.CantidadCuotas,
                FechaPrimerVencimiento = plan.FechaPrimerVencimiento,
                Periodicidad = plan.Periodicidad,
                Observaciones = plan.Observaciones,
                Cuotas = plan.Cuotas.OrderBy(c => c.NumeroCuota).Select(MapCuota).ToList()
            };
        }

        private static CuotaResponse MapCuota(CuotaComercial cuota)
        {
            return new CuotaResponse
            {
                Id = cuota.Id,
                PlanPagoId = cuota.PlanPagoId,
                NumeroCuota = cuota.NumeroCuota,
                TipoCuota = cuota.TipoCuota,
                FechaVencimiento = cuota.FechaVencimiento,
                ImporteOriginal = cuota.ImporteOriginal,
                ImportePagado = cuota.ImportePagado,
                SaldoPendiente = cuota.SaldoPendiente,
                Estado = cuota.Estado
            };
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

        private static AcuerdoResponse MapAcuerdo(AcuerdoComercial acuerdo)
        {
            return new AcuerdoResponse
            {
                Id = acuerdo.Id,
                ClienteExternoId = acuerdo.ClienteExternoId,
                ObraExternaId = acuerdo.ObraExternaId,
                NumeroAcuerdo = acuerdo.NumeroAcuerdo,
                FechaAcuerdo = acuerdo.FechaAcuerdo,
                Descripcion = acuerdo.Descripcion,
                MontoTotal = acuerdo.MontoTotal,
                Estado = acuerdo.Estado,
                ViaOperacion = acuerdo.ViaOperacion,
                Observaciones = acuerdo.Observaciones,
                FechaAlta = acuerdo.FechaAlta,
                UsuarioAlta = acuerdo.UsuarioAlta
            };
        }

        private static readonly Expression<Func<AcuerdoComercial, AcuerdoResponse>> MapAcuerdoExpression = a => new AcuerdoResponse
        {
            Id = a.Id,
            ClienteExternoId = a.ClienteExternoId,
            ObraExternaId = a.ObraExternaId,
            NumeroAcuerdo = a.NumeroAcuerdo,
            FechaAcuerdo = a.FechaAcuerdo,
            Descripcion = a.Descripcion,
            MontoTotal = a.MontoTotal,
            Estado = a.Estado,
            ViaOperacion = a.ViaOperacion,
            Observaciones = a.Observaciones,
            FechaAlta = a.FechaAlta,
            UsuarioAlta = a.UsuarioAlta
        };

        private static IEnumerable<CuotaComercial> BuildCuotas(CreatePlanPagoRequest request, decimal totalRemanente)
        {
            var cuotas = new List<CuotaComercial>();
            var baseCuota = Math.Round(totalRemanente / request.CantidadCuotas, 2);
            var asignado = baseCuota * request.CantidadCuotas;
            var diferencia = totalRemanente - asignado;

            for (var i = 1; i <= request.CantidadCuotas; i++)
            {
                var importe = baseCuota;
                if (i == request.CantidadCuotas)
                {
                    importe += diferencia;
                }

                cuotas.Add(new CuotaComercial
                {
                    NumeroCuota = i,
                    TipoCuota = TipoCuota.Cuota,
                    FechaVencimiento = GetFechaVencimiento(request.FechaPrimerVencimiento, request.Periodicidad, i - 1),
                    ImporteOriginal = importe,
                    ImportePagado = 0,
                    SaldoPendiente = importe,
                    Estado = CuotaEstado.Pendiente
                });
            }

            return cuotas;
        }

        private static DateTime GetFechaVencimiento(DateTime baseDate, string periodicidad, int offset)
        {
            var normalized = periodicidad?.Trim().ToLowerInvariant() ?? string.Empty;
            return normalized switch
            {
                "quincenal" => baseDate.AddDays(15 * offset),
                "semanal" => baseDate.AddDays(7 * offset),
                "anual" => baseDate.AddYears(offset),
                _ => baseDate.AddMonths(offset)
            };
        }

        private static void UpdateCuotaEstado(CuotaComercial cuota)
        {
            if (cuota.Estado == CuotaEstado.Anulada || cuota.Estado == CuotaEstado.Pagada)
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

            if (cuota.ImportePagado > 0)
            {
                cuota.Estado = CuotaEstado.Parcial;
                return;
            }

            cuota.Estado = CuotaEstado.Pendiente;
        }

        private static SaldoComercialResponse MapSaldo(string externoId, IEnumerable<AcuerdoComercial> acuerdos)
        {
            var totalPrometido = acuerdos.Sum(a => a.MontoTotal);
            var totalPagado = acuerdos.SelectMany(a => a.Pagos.SelectMany(p => p.Aplicaciones)).Sum(x => x.ImporteAplicado);
            return new SaldoComercialResponse
            {
                ExternoId = externoId,
                TotalPrometido = totalPrometido,
                TotalPagado = totalPagado,
                SaldoRestante = Math.Max(totalPrometido - totalPagado, 0)
            };
        }
    }
}
