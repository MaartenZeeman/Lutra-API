namespace Lutra.Application.Verspakketten;

public sealed partial class ImportVerspakket
{
    public sealed record Response
    {
        public required Guid Id { get; init; }

        /// <summary>True when a new verspakket was created; false when an existing one was matched.</summary>
        public required bool Created { get; init; }
    }
}
