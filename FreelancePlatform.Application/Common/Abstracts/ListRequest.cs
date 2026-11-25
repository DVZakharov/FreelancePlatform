namespace FreelancePlatform.Application.Common.Abstracts;

public abstract class ListRequest
{
    public int PageSize { get; set; } = 10;
    public int PageNumber { get; set; } = 1;
}
