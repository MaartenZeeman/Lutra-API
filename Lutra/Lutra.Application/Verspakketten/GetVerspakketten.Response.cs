using Lutra.Application.Models.Verspakketten;

namespace Lutra.Application.Verspakketten
{
    public sealed partial class GetVerspakketten
    {
        public sealed class Response
        {
            public required IEnumerable<Verspakket> Verspakketten { get; set; }
        }
    }
}
