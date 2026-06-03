namespace BudgetControl.Api.Models.Commercial
{
    public enum AcuerdoEstado
    {
        Borrador,
        Aprobado,
        EnCurso,
        Finalizado,
        Anulado
    }

    public enum ViaOperacion
    {
        Via1,
        Via2
    }

    public enum TipoCuota
    {
        Anticipo,
        Cuota,
        Refuerzo,
        Ajuste,
        Adicional
    }

    public enum CuotaEstado
    {
        Pendiente,
        Parcial,
        Pagada,
        Vencida,
        Anulada
    }

    public enum TipoAjuste
    {
        CambioImporte,
        CambioVencimiento,
        CambioImporteYVencimiento,
        NuevaCuota,
        AnulacionCuota
    }

    public enum PagoEstado
    {
        Registrado,
        Aplicado,
        Anulado
    }
}
