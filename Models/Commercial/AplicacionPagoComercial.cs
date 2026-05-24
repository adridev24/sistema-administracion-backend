namespace BudgetControl.Api.Models.Commercial
{
    public class AplicacionPagoComercial
    {
        public int Id { get; set; }
        public int PagoComercialId { get; set; }
        public int CuotaComercialId { get; set; }
        public decimal ImporteAplicado { get; set; }
        public DateTime FechaAplicacion { get; set; }

        public PagoComercial PagoComercial { get; set; } = null!;
        public CuotaComercial CuotaComercial { get; set; } = null!;
    }
}
