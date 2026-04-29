namespace Lutra.Application.Verspakketten;

public sealed partial class AddBeoordeling
{
    public sealed record Response
    {
        public required Guid Id { get; init; }
    }
}