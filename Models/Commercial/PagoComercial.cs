using System.ComponentModel.DataAnnotations;

namespace BudgetControl.Api.Models.Commercial
{
    public class PagoComercial
    {
        public int Id { get; set; }

        [Required]
        public string ClienteExternoId { get; set; } = null!;

        [Required]
        public string ObraExternaId { get; set; } = null!;

        public int AcuerdoComercialId { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal ImporteTotal { get; set; }
        public string MedioPago { get; set; } = null!;
        public string? Observaciones { get; set; }
        public PagoEstado Estado { get; set; }

        public AcuerdoComercial AcuerdoComercial { get; set; } = null!;
        public ICollection<AplicacionPagoComercial> Aplicaciones { get; set; } = new List<AplicacionPagoComercial>();
    }
}
