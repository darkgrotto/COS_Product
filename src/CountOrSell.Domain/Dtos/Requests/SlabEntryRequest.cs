namespace CountOrSell.Domain.Dtos.Requests;

public class SlabEntryRequest
{
    public string CardIdentifier { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public string GradingAgency { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public int? SerialNumber { get; set; }
    public int? PrintRunTotal { get; set; }
    public string Condition { get; set; } = string.Empty;
    public bool Autographed { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public decimal AcquisitionPrice { get; set; }
    public string? Notes { get; set; }
}
