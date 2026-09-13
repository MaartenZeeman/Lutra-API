using System.Text;
using Cortex.Mediator;
using Cortex.Mediator.Commands;
using Lutra.Application.Exceptions;
using Lutra.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lutra.Application.Verspakketten;

public sealed partial class ImportVerspakket
{
    public sealed class Handler(
        ILutraDbContext context,
        IVerspakketProductExtractor extractor,
        IMediator mediator) : ICommandHandler<Command, Response>
    {
        private static readonly Dictionary<string, string> HostSupermarkets = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ah.nl"] = "Albert Heijn",
            ["allerhande.nl"] = "Albert Heijn",
            ["jumbo.com"] = "Jumbo",
            ["poiesz.nl"] = "Poiesz",
            ["lidl.nl"] = "Lidl"
        };

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!VerspakketUrlNormalizer.TryNormalize(request.Url, out var normalizedUrl))
            {
                throw new ValidationException("De opgegeven URL is ongeldig. Gebruik een absolute https-URL.");
            }

            // Check before any network call so an already imported product never depends on the retailer being online.
            var existing = await context.Verspaketten
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.BronUrl == normalizedUrl, cancellationToken);

            if (existing is not null)
            {
                return new Response { Id = existing.Id, Created = false };
            }

            var extracted = await extractor.ExtractAsync(normalizedUrl, cancellationToken);
            ValidateExtracted(extracted);

            var supermarkten = await context.Supermarkten.AsNoTracking().ToListAsync(cancellationToken);
            var supermarkt = ResolveSupermarkt(supermarkten, extracted.SupermarktNaam, normalizedUrl)
                ?? throw new ValidationException($"Supermarkt '{extracted.SupermarktNaam}' kon niet worden gekoppeld aan een bekende supermarkt.");

            var naam = extracted.Naam.Trim();

            var legacyMatches = await context.Verspaketten
                .Where(v => v.BronUrl == null
                    && v.SupermarktId == supermarkt.Id
                    && v.Naam.ToLower() == naam.ToLower())
                .ToListAsync(cancellationToken);

            if (legacyMatches.Count > 1)
            {
                throw new ConflictException($"Meerdere bestaande verspakketten komen overeen met '{naam}'. Import afgebroken om een verkeerde koppeling te voorkomen.");
            }

            if (legacyMatches.Count == 1)
            {
                var legacy = await context.Verspaketten.FirstAsync(v => v.Id == legacyMatches[0].Id, cancellationToken);
                legacy.BronUrl = normalizedUrl;
                legacy.ModifiedAt = DateTime.UtcNow;

                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                    return new Response { Id = legacy.Id, Created = false };
                }
                catch (DbUpdateException)
                {
                    var raced = await context.Verspaketten.AsNoTracking()
                        .FirstOrDefaultAsync(v => v.BronUrl == normalizedUrl, cancellationToken);

                    if (raced is not null)
                    {
                        return new Response { Id = raced.Id, Created = false };
                    }

                    throw;
                }
            }

            var command = new CreateVerspakket.Command(
                naam,
                extracted.PrijsInCenten,
                extracted.AantalPersonen!.Value,
                supermarkt.Id,
                null,
                extracted.Fotos.Count > 0 ? extracted.Fotos : null,
                extracted.Ingredienten.Count > 0 ? extracted.Ingredienten : null,
                extracted.Voedingswaarden.Count > 0 ? extracted.Voedingswaarden : null,
                extracted.Allergenen.Count > 0 ? extracted.Allergenen : null,
                normalizedUrl);

            try
            {
                var created = await mediator.SendCommandAsync<CreateVerspakket.Command, CreateVerspakket.Response>(command, cancellationToken);
                return new Response { Id = created.Id, Created = true };
            }
            catch (DbUpdateException)
            {
                // The unique index on BronUrl is the final safeguard against concurrent imports.
                var raced = await context.Verspaketten.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.BronUrl == normalizedUrl, cancellationToken);

                if (raced is not null)
                {
                    return new Response { Id = raced.Id, Created = false };
                }

                throw;
            }
        }

        private static void ValidateExtracted(Models.Verspakketten.ExtractedVerspakket extracted)
        {
            if (string.IsNullOrWhiteSpace(extracted.Naam))
            {
                throw new UnprocessableException("De productpagina bevatte geen productnaam.");
            }

            if (extracted.Naam.Trim().Length > 50)
            {
                throw new UnprocessableException("De productnaam is te lang (maximaal 50 tekens).");
            }

            if (extracted.AantalPersonen is null or < 1 or > 10)
            {
                throw new UnprocessableException("Het aantal personen ontbreekt of is ongeldig.");
            }

            if (extracted.PrijsInCenten is < 0)
            {
                throw new UnprocessableException("De prijs mag niet negatief zijn.");
            }
        }

        private static Domain.Entities.Supermarkt? ResolveSupermarkt(
            IReadOnlyList<Domain.Entities.Supermarkt> supermarkten,
            string? aiName,
            string normalizedUrl)
        {
            var candidates = new List<string?>();

            if (Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri))
            {
                foreach (var (host, name) in HostSupermarkets)
                {
                    if (uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase)
                        || uri.Host.EndsWith($".{host}", StringComparison.OrdinalIgnoreCase))
                    {
                        candidates.Add(name);
                        break;
                    }
                }
            }

            candidates.Add(aiName);

            foreach (var candidate in candidates)
            {
                var match = MatchByName(supermarkten, candidate);
                if (match is not null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Domain.Entities.Supermarkt? MatchByName(
            IReadOnlyList<Domain.Entities.Supermarkt> supermarkten,
            string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var normalized = NormalizeName(name);
            return supermarkten.FirstOrDefault(s => NormalizeName(s.Naam) == normalized);
        }

        private static string NormalizeName(string value)
        {
            var builder = new StringBuilder(value.Length);

            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString() switch
            {
                "ah" => "albertheijn",
                var result => result
            };
        }
    }
}
