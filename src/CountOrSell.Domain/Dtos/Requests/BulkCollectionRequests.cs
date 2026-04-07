namespace CountOrSell.Domain.Dtos.Requests;

public class BulkIdsRequest
{
    public List<Guid> Ids { get; set; } = [];
}

public class BulkSetTreatmentRequest
{
    public List<Guid> Ids { get; set; } = [];
    public string Treatment { get; set; } = string.Empty;
}

public class BulkSetAcquisitionDateRequest
{
    public List<Guid> Ids { get; set; } = [];
    public DateOnly AcquisitionDate { get; set; }
}
