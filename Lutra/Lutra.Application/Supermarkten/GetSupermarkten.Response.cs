using Lutra.Application.Models.Supermarkten;

namespace Lutra.Application.Supermarkten
{
    public sealed partial class GetSupermarkten
    {
        public sealed class Response
        {
            public required IEnumerable<Supermarkt> Supermarkten { get; set; }
        }
    }
}
