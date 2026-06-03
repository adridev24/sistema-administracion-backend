using BudgetControl.Api.DTOs.Commercial;

namespace BudgetControl.Api.Services.Commercial
{
    public interface IComercialService
    {
        Task<AcuerdoResponse> CreateAcuerdoAsync(CreateAcuerdoRequest request);
        Task<AcuerdoDetalleResponse?> GetAcuerdoDetalleAsync(int id);
        Task<AcuerdoResponse> AprobarAcuerdoAsync(int acuerdoId);
        Task<IEnumerable<AcuerdoResponse>> GetAcuerdosPorClienteAsync(string clienteExternoId);
        Task<IEnumerable<AcuerdoResponse>> GetAcuerdosPorObraAsync(string obraExternoId);
        Task<PlanPagoResponse> CrearPlanPagoAsync(int acuerdoId, CreatePlanPagoRequest request);
        Task<PlanPagoResponse> ActualizarPlanPagoAsync(int acuerdoId, UpdatePlanPagoRequest request);
        Task<EstadoComercialResponse> GetEstadoComercialAsync(int acuerdoId);
        Task<SaldoComercialResponse> GetSaldoComercialClienteAsync(string clienteExternoId);
        Task<SaldoComercialResponse> GetSaldoComercialObraAsync(string obraExternaId);
        Task<ReporteComercialResumenResponse> GetReporteComercialResumenAsync(DateTime periodoDesde, DateTime periodoHasta);
        Task<IEnumerable<CuotaResponse>> GetCuotasVencidasAsync();
        Task<IEnumerable<CuotaResponse>> GetCuotasPendientesAsync();
        Task<CuotaResponse> AjustarCuotaAsync(int cuotaId, AjusteCuotaRequest request);
        Task<CuotaResponse> AgregarCuotaAjusteAsync(int planPagoId, AddCuotaAjusteRequest request);
        Task<IEnumerable<AjusteCuotaResponse>> GetHistorialAjustesPorCuotaAsync(int cuotaId);
        Task<IEnumerable<AjusteCuotaResponse>> GetHistorialAjustesPorAcuerdoAsync(int acuerdoId);
    }
}
