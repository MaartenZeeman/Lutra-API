using Lutra.Application.Models.Verspakketten;

namespace Lutra.Application.Verspakketten
{
    public sealed partial class GetVerspakket
    {
        public sealed class Response
        {
            public required Verspakket Verspakket { get; set; }
        }
    }
}
