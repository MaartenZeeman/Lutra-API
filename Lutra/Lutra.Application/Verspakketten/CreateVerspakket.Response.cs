namespace Lutra.Application.Verspakketten;

public sealed partial class CreateVerspakket
{
    public sealed record Response
    {
        public required Guid Id { get; init; }
    }
}
