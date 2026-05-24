using System.ComponentModel.DataAnnotations;
using BudgetControl.Api.Models.Commercial;

namespace BudgetControl.Api.DTOs.Commercial
{
    public class CreateAcuerdoRequest
    {
        [Required]
        public string ClienteExternoId { get; set; } = null!;

        [Required]
        public string ObraExternaId { get; set; } = null!;

        [Required]
        public string NumeroAcuerdo { get; set; } = null!;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal MontoTotal { get; set; }

        [Required]
        public DateTime FechaAcuerdo { get; set; }

        public string? Descripcion { get; set; }
        public AcuerdoEstado Estado { get; set; }
        public ViaOperacion ViaOperacion { get; set; }
        public string? Observaciones { get; set; }

        [Required]
        public string UsuarioAlta { get; set; } = null!;
    }

    public class CreatePlanPagoRequest
    {
        [Required]
        public bool TieneAnticipo { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MontoAnticipo { get; set; }

        [Range(1, int.MaxValue)]
        public int CantidadCuotas { get; set; }

        [Required]
        public DateTime FechaPrimerVencimiento { get; set; }

        [Required]
        public string Periodicidad { get; set; } = null!;
        public string? Observaciones { get; set; }
    }

    public class CreatePagoComercialRequest
    {
        [Required]
        public string ClienteExternoId { get; set; } = null!;

        [Required]
        public string ObraExternaId { get; set; } = null!;

        [Required]
        public int AcuerdoComercialId { get; set; }

        [Required]
        public DateTime FechaPago { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal ImporteTotal { get; set; }

        [Required]
        public string MedioPago { get; set; } = null!;
        public string? Observaciones { get; set; }
        public List<AplicacionPagoRequest> Aplicaciones { get; set; } = new List<AplicacionPagoRequest>();
    }

    public class AplicacionPagoRequest
    {
        [Required]
        public int CuotaComercialId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal ImporteAplicado { get; set; }
    }

    public class AplicarPagoRequest
    {
        [Required]
        public List<AplicacionPagoRequest> Aplicaciones { get; set; } = new List<AplicacionPagoRequest>();
    }
}
