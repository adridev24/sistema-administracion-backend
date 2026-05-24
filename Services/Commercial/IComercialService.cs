using BudgetControl.Api.DTOs.Commercial;

namespace BudgetControl.Api.Services.Commercial
{
    public interface IComercialService
    {
        Task<AcuerdoResponse> CreateAcuerdoAsync(CreateAcuerdoRequest request);
        Task<AcuerdoDetalleResponse?> GetAcuerdoDetalleAsync(int id);
        Task<IEnumerable<AcuerdoResponse>> GetAcuerdosPorClienteAsync(string clienteExternoId);
        Task<IEnumerable<AcuerdoResponse>> GetAcuerdosPorObraAsync(string obraExternoId);
        Task<PlanPagoResponse> CrearPlanPagoAsync(int acuerdoId, CreatePlanPagoRequest request);
        Task<EstadoComercialResponse> GetEstadoComercialAsync(int acuerdoId);
        Task<SaldoComercialResponse> GetSaldoComercialClienteAsync(string clienteExternoId);
        Task<SaldoComercialResponse> GetSaldoComercialObraAsync(string obraExternoId);
        Task<IEnumerable<CuotaResponse>> GetCuotasVencidasAsync();
        Task<IEnumerable<CuotaResponse>> GetCuotasPendientesAsync();
    }
}
