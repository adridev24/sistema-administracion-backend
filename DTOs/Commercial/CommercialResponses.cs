using BudgetControl.Api.Models.Commercial;

namespace BudgetControl.Api.DTOs.Commercial
{
    public class AcuerdoResponse
    {
        public int Id { get; set; }
        public string ClienteExternoId { get; set; } = null!;
        public string ObraExternaId { get; set; } = null!;
        public string NumeroAcuerdo { get; set; } = null!;
        public DateTime FechaAcuerdo { get; set; }
        public string? Descripcion { get; set; }
        public decimal MontoTotal { get; set; }
        public AcuerdoEstado Estado { get; set; }
        public ViaOperacion ViaOperacion { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaAlta { get; set; }
        public string UsuarioAlta { get; set; } = null!;
    }

    public class AcuerdoDetalleResponse : AcuerdoResponse
    {
        public PlanPagoResponse? PlanPago { get; set; }
        public List<PagoComercialResponse> Pagos { get; set; } = new List<PagoComercialResponse>();
    }

    public class PlanPagoResponse
    {
        public int Id { get; set; }
        public int AcuerdoComercialId { get; set; }
        public bool TieneAnticipo { get; set; }
        public decimal MontoAnticipo { get; set; }
        public int CantidadCuotas { get; set; }
        public DateTime FechaPrimerVencimiento { get; set; }
        public string Periodicidad { get; set; } = null!;
        public string? Observaciones { get; set; }
        public List<CuotaResponse> Cuotas { get; set; } = new List<CuotaResponse>();
    }

    public class CuotaResponse
    {
        public int Id { get; set; }
        public int PlanPagoId { get; set; }
        public int NumeroCuota { get; set; }
        public TipoCuota TipoCuota { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal ImporteOriginal { get; set; }
        public decimal ImportePagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public CuotaEstado Estado { get; set; }
    }

    public class AjusteCuotaResponse
    {
        public int Id { get; set; }
        public int CuotaComercialId { get; set; }
        public int PlanPagoId { get; set; }
        public int AcuerdoComercialId { get; set; }
        public TipoAjuste TipoAjuste { get; set; }
        public decimal? ImporteAnterior { get; set; }
        public decimal? ImporteNuevo { get; set; }
        public DateTime? FechaVencimientoAnterior { get; set; }
        public DateTime? FechaVencimientoNueva { get; set; }
        public string Motivo { get; set; } = null!;
        public DateTime FechaAjuste { get; set; }
        public string UsuarioAjuste { get; set; } = null!;
    }

    public class PagoComercialResponse
    {
        public int Id { get; set; }
        public string ClienteExternoId { get; set; } = null!;
        public string ObraExternaId { get; set; } = null!;
        public int AcuerdoComercialId { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal ImporteTotal { get; set; }
        public string MedioPago { get; set; } = null!;
        public string? Observaciones { get; set; }
        public PagoEstado Estado { get; set; }
        public List<AplicacionPagoResponse> Aplicaciones { get; set; } = new List<AplicacionPagoResponse>();
    }

    public class AplicacionPagoResponse
    {
        public int Id { get; set; }
        public int PagoComercialId { get; set; }
        public int CuotaComercialId { get; set; }
        public decimal ImporteAplicado { get; set; }
        public DateTime FechaAplicacion { get; set; }
    }

    public class EstadoComercialResponse
    {
        public int AcuerdoComercialId { get; set; }
        public decimal TotalPrometido { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoRestante { get; set; }
    }

    public class SaldoComercialResponse
    {
        public string ExternoId { get; set; } = null!;
        public decimal TotalPrometido { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoRestante { get; set; }
    }

    public class ReporteComercialResumenResponse
    {
        public DateTime PeriodoDesde { get; set; }
        public DateTime PeriodoHasta { get; set; }
        public decimal TotalAcordadoActivo { get; set; }
        public decimal TotalCobradoPeriodo { get; set; }
        public decimal TotalPorCobrarPeriodo { get; set; }
        public decimal TotalVencido { get; set; }
        public decimal SaldoTotalClientes { get; set; }
        public int AcuerdosActivos { get; set; }
        public int CuotasPendientesPeriodo { get; set; }
        public int CuotasVencidas { get; set; }
        public List<ClienteDeudaReporteResponse> ClientesConDeuda { get; set; } = new();
        public List<CuotaReporteResponse> ProximosVencimientos { get; set; } = new();
    }

    public class ClienteDeudaReporteResponse
    {
        public string ClienteExternoId { get; set; } = null!;
        public decimal TotalAcordado { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
        public int AcuerdosActivos { get; set; }
    }

    public class CuotaReporteResponse
    {
        public int CuotaId { get; set; }
        public int AcuerdoComercialId { get; set; }
        public string NumeroAcuerdo { get; set; } = null!;
        public string ClienteExternoId { get; set; } = null!;
        public string ObraExternaId { get; set; } = null!;
        public DateTime FechaVencimiento { get; set; }
        public decimal SaldoPendiente { get; set; }
        public CuotaEstado Estado { get; set; }
    }
}
