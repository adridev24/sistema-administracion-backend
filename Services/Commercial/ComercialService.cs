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
                .Include(a => a.PlanPago)
                    .ThenInclude(p => p!.Cuotas)
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

        public async Task<AcuerdoResponse> AprobarAcuerdoAsync(int acuerdoId)
        {
            var acuerdo = await _db.AcuerdosComerciales
                .FirstOrDefaultAsync(a => a.Id == acuerdoId);

            if (acuerdo == null)
            {
                throw new InvalidOperationException("Acuerdo comercial no encontrado.");
            }

            if (acuerdo.Estado == AcuerdoEstado.Aprobado)
            {
                throw new InvalidOperationException("El acuerdo ya se encuentra aprobado.");
            }

            if (acuerdo.Estado == AcuerdoEstado.Finalizado || acuerdo.Estado == AcuerdoEstado.Anulado)
            {
                throw new InvalidOperationException("No se puede aprobar un acuerdo finalizado o anulado.");
            }

            if (acuerdo.Estado != AcuerdoEstado.Borrador)
            {
                throw new InvalidOperationException("Solo los acuerdos en estado Borrador pueden aprobarse.");
            }

            acuerdo.Estado = AcuerdoEstado.Aprobado;
            await _db.SaveChangesAsync();

            return MapAcuerdo(acuerdo);
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

        public async Task<PlanPagoResponse> ActualizarPlanPagoAsync(int acuerdoId, UpdatePlanPagoRequest request)
        {
            var acuerdo = await _db.AcuerdosComerciales
                .Include(a => a.PlanPago!)
                    .ThenInclude(p => p.Cuotas)
                .FirstOrDefaultAsync(a => a.Id == acuerdoId);

            if (acuerdo == null)
            {
                throw new InvalidOperationException("Acuerdo comercial no encontrado.");
            }

            if (acuerdo.Estado != AcuerdoEstado.Borrador)
            {
                throw new InvalidOperationException("Solo los acuerdos en estado Borrador pueden personalizar su plan de pago.");
            }

            if (acuerdo.PlanPago == null)
            {
                throw new InvalidOperationException("El acuerdo no tiene un plan de pago asociado.");
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
            }
            else if (request.MontoAnticipo != 0)
            {
                throw new InvalidOperationException("No puede enviar un monto de anticipo cuando no tiene anticipo.");
            }

            var cuotasById = acuerdo.PlanPago.Cuotas.ToDictionary(c => c.Id);
            foreach (var cuotaRequest in request.Cuotas)
            {
                if (!cuotasById.TryGetValue(cuotaRequest.Id, out var cuota))
                {
                    throw new InvalidOperationException($"La cuota con id {cuotaRequest.Id} no se encuentra en el plan.");
                }

                if (cuota.ImportePagado > cuotaRequest.ImporteOriginal)
                {
                    throw new InvalidOperationException("El importe original no puede ser menor al importe ya pagado.");
                }

                cuota.FechaVencimiento = cuotaRequest.FechaVencimiento;
                cuota.ImporteOriginal = cuotaRequest.ImporteOriginal;
                cuota.SaldoPendiente = Math.Max(cuota.ImporteOriginal - cuota.ImportePagado, 0);
                if (cuota.SaldoPendiente <= 0)
                {
                    cuota.Estado = CuotaEstado.Pagada;
                }
                else if (cuota.FechaVencimiento < DateTime.UtcNow)
                {
                    cuota.Estado = CuotaEstado.Vencida;
                }
                else
                {
                    cuota.Estado = CuotaEstado.Pendiente;
                }
            }

            acuerdo.PlanPago.TieneAnticipo = request.TieneAnticipo;
            acuerdo.PlanPago.MontoAnticipo = request.MontoAnticipo;
            acuerdo.PlanPago.CantidadCuotas = request.CantidadCuotas;
            acuerdo.PlanPago.FechaPrimerVencimiento = request.FechaPrimerVencimiento;
            acuerdo.PlanPago.Periodicidad = request.Periodicidad;
            acuerdo.PlanPago.Observaciones = request.Observaciones;

            var totalPlan = request.MontoAnticipo + acuerdo.PlanPago.Cuotas
                .Where(c => c.TipoCuota == TipoCuota.Cuota)
                .Sum(c => c.ImporteOriginal);

            if (totalPlan <= 0)
            {
                throw new InvalidOperationException("El monto total del acuerdo debe ser mayor a cero.");
            }

            acuerdo.MontoTotal = totalPlan;

            await _db.SaveChangesAsync();

            return MapPlanPago(acuerdo.PlanPago);
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

        public async Task<ReporteComercialResumenResponse> GetReporteComercialResumenAsync(DateTime periodoDesde, DateTime periodoHasta)
        {
            var desde = DateTime.SpecifyKind(periodoDesde.Date, DateTimeKind.Utc);
            var hasta = DateTime.SpecifyKind(periodoHasta.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            var hoy = DateTime.UtcNow.Date;

            var acuerdosActivos = await _db.AcuerdosComerciales
                .Where(a => a.Estado != AcuerdoEstado.Anulado && a.Estado != AcuerdoEstado.Finalizado)
                .Include(a => a.Pagos)
                    .ThenInclude(p => p.Aplicaciones)
                .ToListAsync();

            var cuotasPendientes = await _db.CuotasComerciales
                .Include(c => c.PlanPago)
                    .ThenInclude(p => p.AcuerdoComercial)
                .Where(c =>
                    c.SaldoPendiente > 0 &&
                    c.Estado != CuotaEstado.Pagada &&
                    c.Estado != CuotaEstado.Anulada &&
                    c.PlanPago.AcuerdoComercial.Estado != AcuerdoEstado.Anulado &&
                    c.PlanPago.AcuerdoComercial.Estado != AcuerdoEstado.Finalizado)
                .ToListAsync();

            cuotasPendientes.ForEach(UpdateCuotaEstado);
            await _db.SaveChangesAsync();

            var pagosPeriodo = await _db.PagosComerciales
                .Where(p => p.Estado != PagoEstado.Anulado && p.FechaPago >= desde && p.FechaPago <= hasta)
                .ToListAsync();

            var deudaPorCliente = acuerdosActivos
                .GroupBy(a => a.ClienteExternoId)
                .Select(group =>
                {
                    var totalAcordado = group.Sum(a => a.MontoTotal);
                    var totalPagado = group.SelectMany(a => a.Pagos.SelectMany(p => p.Aplicaciones)).Sum(x => x.ImporteAplicado);
                    return new ClienteDeudaReporteResponse
                    {
                        ClienteExternoId = group.Key,
                        TotalAcordado = totalAcordado,
                        TotalPagado = totalPagado,
                        SaldoPendiente = Math.Max(totalAcordado - totalPagado, 0),
                        AcuerdosActivos = group.Count()
                    };
                })
                .Where(item => item.SaldoPendiente > 0)
                .OrderByDescending(item => item.SaldoPendiente)
                .ToList();

            var clientesConDeuda = deudaPorCliente
                .Take(10)
                .ToList();

            var proximosVencimientos = cuotasPendientes
                .Where(c => c.FechaVencimiento >= hoy)
                .OrderBy(c => c.FechaVencimiento)
                .Take(10)
                .Select(MapCuotaReporte)
                .ToList();

            return new ReporteComercialResumenResponse
            {
                PeriodoDesde = desde,
                PeriodoHasta = hasta,
                TotalAcordadoActivo = acuerdosActivos.Sum(a => a.MontoTotal),
                TotalCobradoPeriodo = pagosPeriodo.Sum(p => p.ImporteTotal),
                TotalPorCobrarPeriodo = cuotasPendientes
                    .Where(c => c.FechaVencimiento >= desde && c.FechaVencimiento <= hasta)
                    .Sum(c => c.SaldoPendiente),
                TotalVencido = cuotasPendientes
                    .Where(c => c.FechaVencimiento.Date < hoy)
                    .Sum(c => c.SaldoPendiente),
                SaldoTotalClientes = deudaPorCliente.Sum(c => c.SaldoPendiente),
                AcuerdosActivos = acuerdosActivos.Count,
                CuotasPendientesPeriodo = cuotasPendientes.Count(c => c.FechaVencimiento >= desde && c.FechaVencimiento <= hasta),
                CuotasVencidas = cuotasPendientes.Count(c => c.FechaVencimiento.Date < hoy),
                ClientesConDeuda = clientesConDeuda,
                ProximosVencimientos = proximosVencimientos
            };
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

        public async Task<CuotaResponse> AjustarCuotaAsync(int cuotaId, AjusteCuotaRequest request)
        {
            var cuota = await _db.CuotasComerciales
                .Include(c => c.PlanPago)
                    .ThenInclude(p => p.AcuerdoComercial)
                .FirstOrDefaultAsync(c => c.Id == cuotaId);

            if (cuota == null)
            {
                throw new InvalidOperationException("La cuota comercial no fue encontrada.");
            }

            if (cuota.Estado == CuotaEstado.Pagada)
            {
                throw new InvalidOperationException("Una cuota pagada no puede ajustarse.");
            }

            if (string.IsNullOrWhiteSpace(request.Motivo))
            {
                throw new InvalidOperationException("El motivo del ajuste es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(request.Usuario))
            {
                throw new InvalidOperationException("El usuario del ajuste es obligatorio.");
            }

            if (!request.NuevoImporteOriginal.HasValue && !request.NuevaFechaVencimiento.HasValue)
            {
                throw new InvalidOperationException("Debe especificar un nuevo importe o una nueva fecha de vencimiento.");
            }

            if (request.NuevoImporteOriginal.HasValue)
            {
                if (request.NuevoImporteOriginal.Value <= 0)
                {
                    throw new InvalidOperationException("El importe original debe ser mayor a cero.");
                }

                if (request.NuevoImporteOriginal.Value < cuota.ImportePagado)
                {
                    throw new InvalidOperationException("El nuevo importe original no puede ser menor al importe ya pagado.");
                }
            }

            var importeAnterior = cuota.ImporteOriginal;
            var fechaAnterior = cuota.FechaVencimiento;
            var importeNuevo = request.NuevoImporteOriginal ?? cuota.ImporteOriginal;

            cuota.ImporteOriginal = importeNuevo;
            if (request.NuevaFechaVencimiento.HasValue)
            {
                cuota.FechaVencimiento = request.NuevaFechaVencimiento.Value;
            }

            if (cuota.PlanPago?.AcuerdoComercial != null && request.NuevoImporteOriginal.HasValue)
            {
                cuota.PlanPago.AcuerdoComercial.MontoTotal += importeNuevo - importeAnterior;
            }

            cuota.SaldoPendiente = Math.Max(cuota.ImporteOriginal - cuota.ImportePagado, 0);
            UpdateCuotaEstado(cuota);

            var tipoAjuste = request.NuevoImporteOriginal.HasValue && request.NuevaFechaVencimiento.HasValue
                ? TipoAjuste.CambioImporteYVencimiento
                : request.NuevoImporteOriginal.HasValue
                    ? TipoAjuste.CambioImporte
                    : TipoAjuste.CambioVencimiento;

            var ajuste = new AjusteCuotaComercial
            {
                CuotaComercialId = cuota.Id,
                PlanPagoId = cuota.PlanPagoId,
                AcuerdoComercialId = cuota.PlanPago.AcuerdoComercialId,
                TipoAjuste = tipoAjuste,
                ImporteAnterior = importeAnterior,
                ImporteNuevo = importeNuevo,
                FechaVencimientoAnterior = fechaAnterior,
                FechaVencimientoNueva = cuota.FechaVencimiento,
                Motivo = request.Motivo,
                FechaAjuste = DateTime.UtcNow,
                UsuarioAjuste = request.Usuario
            };

            _db.AjustesCuotaComerciales.Add(ajuste);
            await _db.SaveChangesAsync();

            return MapCuota(cuota);
        }

        public async Task<CuotaResponse> AgregarCuotaAjusteAsync(int planPagoId, AddCuotaAjusteRequest request)
        {
            var plan = await _db.PlanesPago
                .Include(p => p.AcuerdoComercial)
                .Include(p => p.Cuotas)
                .FirstOrDefaultAsync(p => p.Id == planPagoId);

            if (plan == null)
            {
                throw new InvalidOperationException("El plan de pago no fue encontrado.");
            }

            if (plan.AcuerdoComercial == null)
            {
                throw new InvalidOperationException("El acuerdo comercial asociado al plan no se encontró.");
            }

            if (plan.AcuerdoComercial.Estado != AcuerdoEstado.Aprobado && plan.AcuerdoComercial.Estado != AcuerdoEstado.EnCurso)
            {
                throw new InvalidOperationException("Solo se pueden agregar cuotas a planes en acuerdos aprobados o en curso.");
            }

            if (request.TipoCuota != TipoCuota.Ajuste && request.TipoCuota != TipoCuota.Adicional)
            {
                throw new InvalidOperationException("El tipo de cuota debe ser Ajuste o Adicional.");
            }

            if (request.ImporteOriginal <= 0)
            {
                throw new InvalidOperationException("El importe original debe ser mayor a cero.");
            }

            if (string.IsNullOrWhiteSpace(request.Motivo))
            {
                throw new InvalidOperationException("El motivo del ajuste es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(request.Usuario))
            {
                throw new InvalidOperationException("El usuario del ajuste es obligatorio.");
            }

            var numeroCuota = plan.Cuotas.Any() ? plan.Cuotas.Max(c => c.NumeroCuota) + 1 : 1;
            var estado = request.FechaVencimiento.Date < DateTime.UtcNow.Date ? CuotaEstado.Vencida : CuotaEstado.Pendiente;

            var cuota = new CuotaComercial
            {
                PlanPagoId = planPagoId,
                NumeroCuota = numeroCuota,
                TipoCuota = request.TipoCuota,
                FechaVencimiento = request.FechaVencimiento,
                ImporteOriginal = request.ImporteOriginal,
                ImportePagado = 0,
                SaldoPendiente = request.ImporteOriginal,
                Estado = estado
            };

            plan.Cuotas.Add(cuota);
            plan.CantidadCuotas += 1;
            plan.AcuerdoComercial.MontoTotal += request.ImporteOriginal;

            var ajuste = new AjusteCuotaComercial
            {
                CuotaComercial = cuota,
                PlanPagoId = planPagoId,
                AcuerdoComercialId = plan.AcuerdoComercialId,
                TipoAjuste = TipoAjuste.NuevaCuota,
                ImporteAnterior = 0,
                ImporteNuevo = request.ImporteOriginal,
                FechaVencimientoAnterior = null,
                FechaVencimientoNueva = request.FechaVencimiento,
                Motivo = request.Motivo,
                FechaAjuste = DateTime.UtcNow,
                UsuarioAjuste = request.Usuario
            };

            _db.AjustesCuotaComerciales.Add(ajuste);
            await _db.SaveChangesAsync();

            return MapCuota(cuota);
        }

        public async Task<IEnumerable<AjusteCuotaResponse>> GetHistorialAjustesPorCuotaAsync(int cuotaId)
        {
            var ajustes = await _db.AjustesCuotaComerciales
                .Where(a => a.CuotaComercialId == cuotaId)
                .OrderByDescending(a => a.FechaAjuste)
                .ToListAsync();

            return ajustes.Select(MapAjusteCuota).ToList();
        }

        public async Task<IEnumerable<AjusteCuotaResponse>> GetHistorialAjustesPorAcuerdoAsync(int acuerdoId)
        {
            var ajustes = await _db.AjustesCuotaComerciales
                .Where(a => a.AcuerdoComercialId == acuerdoId)
                .OrderByDescending(a => a.FechaAjuste)
                .ToListAsync();

            return ajustes.Select(MapAjusteCuota).ToList();
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

        private static CuotaReporteResponse MapCuotaReporte(CuotaComercial cuota)
        {
            return new CuotaReporteResponse
            {
                CuotaId = cuota.Id,
                AcuerdoComercialId = cuota.PlanPago.AcuerdoComercialId,
                NumeroAcuerdo = cuota.PlanPago.AcuerdoComercial.NumeroAcuerdo,
                ClienteExternoId = cuota.PlanPago.AcuerdoComercial.ClienteExternoId,
                ObraExternaId = cuota.PlanPago.AcuerdoComercial.ObraExternaId,
                FechaVencimiento = cuota.FechaVencimiento,
                SaldoPendiente = cuota.SaldoPendiente,
                Estado = cuota.Estado
            };
        }

        private static AjusteCuotaResponse MapAjusteCuota(AjusteCuotaComercial ajuste)
        {
            return new AjusteCuotaResponse
            {
                Id = ajuste.Id,
                CuotaComercialId = ajuste.CuotaComercialId,
                PlanPagoId = ajuste.PlanPagoId,
                AcuerdoComercialId = ajuste.AcuerdoComercialId,
                TipoAjuste = ajuste.TipoAjuste,
                ImporteAnterior = ajuste.ImporteAnterior,
                ImporteNuevo = ajuste.ImporteNuevo,
                FechaVencimientoAnterior = ajuste.FechaVencimientoAnterior,
                FechaVencimientoNueva = ajuste.FechaVencimientoNueva,
                Motivo = ajuste.Motivo,
                FechaAjuste = ajuste.FechaAjuste,
                UsuarioAjuste = ajuste.UsuarioAjuste
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
