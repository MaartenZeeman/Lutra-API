namespace Lutra.Application.Supermarkten;

public sealed partial class CreateSupermarkt
{
    public sealed record Response
    {
        public required Guid Id { get; init; }
    }
}