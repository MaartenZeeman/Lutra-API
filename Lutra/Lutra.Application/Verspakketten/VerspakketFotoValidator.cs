using Lutra.Application.Exceptions;
using Lutra.Application.Models.Verspakketten;

namespace Lutra.Application.Verspakketten;

/// <summary>
/// Validates and decodes base64 photo payloads so callers cannot store unbounded blobs
/// or trigger an unhandled <see cref="FormatException"/> on malformed input.
/// </summary>
public static class VerspakketFotoValidator
{
    public const int MaxFotos = 10;

    /// <summary>Largest decoded size for a single foto (5 MiB), matching the import limits.</summary>
    public const long MaxFotoBytes = 5_242_880;

    /// <summary>Largest combined decoded size for all fotos of one verspakket (20 MiB), matching the import limits.</summary>
    public const long MaxTotalBytes = 20_971_520;

    private const long MaxEncodedLength = (MaxTotalBytes + 2) / 3 * 4;

    public static IReadOnlyList<(byte[] Data, bool IsMainImage)> DecodeAll(IReadOnlyList<VerspakketFoto>? fotos)
    {
        if (fotos is null || fotos.Count == 0)
        {
            return [];
        }

        if (fotos.Count > MaxFotos)
        {
            throw new ValidationException($"Een verspakket mag maximaal {MaxFotos} foto's hebben.");
        }

        var result = new List<(byte[] Data, bool IsMainImage)>(fotos.Count);
        long total = 0;

        foreach (var foto in fotos)
        {
            var data = Decode(foto.Base64Data);
            total += data.LongLength;

            if (total > MaxTotalBytes)
            {
                throw new ValidationException("De totale fotogrootte is te groot.");
            }

            result.Add((data, foto.IsMainImage));
        }

        return result;
    }

    private static byte[] Decode(string? base64Data)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
        {
            throw new ValidationException("Foto-afbeelding mag niet leeg zijn.");
        }

        if (base64Data.Length > MaxEncodedLength)
        {
            throw new ValidationException("Foto-afbeelding is te groot.");
        }

        byte[] data;

        try
        {
            data = Convert.FromBase64String(base64Data);
        }
        catch (FormatException)
        {
            throw new ValidationException("Foto-afbeelding is geen geldige base64.");
        }

        if (data.LongLength > MaxFotoBytes)
        {
            throw new ValidationException("Foto-afbeelding is te groot.");
        }

        return data;
    }
}