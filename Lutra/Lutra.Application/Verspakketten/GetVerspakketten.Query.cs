using Cortex.Mediator.Queries;

namespace Lutra.Application.Verspakketten;

public sealed partial class GetVerspakketten
{
    public record Query(
        int Skip,
        int Take,
        VerspakketSortField SortField = VerspakketSortField.Naam,
        SortDirection SortDirection = SortDirection.Ascending) : IQuery<Response>;
}
