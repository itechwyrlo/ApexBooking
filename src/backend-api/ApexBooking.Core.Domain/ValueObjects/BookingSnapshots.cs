using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.ValueObjects;

/// <summary>
/// A copy of a service's price at the moment of booking (05 §6). Not linked to the live
/// <c>Service.Price</c> — this is what makes a completed booking a stable historical record even
/// after the service is later re-priced.
/// </summary>
public record PriceSnapshot
{
    public decimal Value { get; }
    public string Currency { get; }

    private PriceSnapshot() : this(0m, "PHP", skipValidation: true) { }

    public PriceSnapshot(decimal value, string currency)
        : this(value, currency, skipValidation: false) { }

    private PriceSnapshot(decimal value, string currency, bool skipValidation)
    {
        if (!skipValidation)
        {
            if (value < 0)
                throw new BusinessRuleBrokenException("Price cannot be negative.");

            if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
                throw new BusinessRuleBrokenException("A valid ISO 4217 currency code is required.");
        }

        Value = value;
        Currency = currency.ToUpperInvariant();
    }
}

/// <summary>
/// A copy of a service's duration and buffers at the moment of booking (05 §6). <see cref="TotalMinutes"/>
/// (service duration + both buffers) is the slot occupancy width used when computing a booking's
/// scheduled end.
/// </summary>
public record DurationSnapshot
{
    public int DurationMinutes { get; }
    public int BufferBeforeMinutes { get; }
    public int BufferAfterMinutes { get; }

    public int TotalMinutes => DurationMinutes + BufferBeforeMinutes + BufferAfterMinutes;

    private DurationSnapshot() : this(1, 0, 0, skipValidation: true) { }

    public DurationSnapshot(int durationMinutes, int bufferBeforeMinutes, int bufferAfterMinutes)
        : this(durationMinutes, bufferBeforeMinutes, bufferAfterMinutes, skipValidation: false) { }

    private DurationSnapshot(int durationMinutes, int bufferBeforeMinutes, int bufferAfterMinutes, bool skipValidation)
    {
        if (!skipValidation)
        {
            if (durationMinutes <= 0)
                throw new BusinessRuleBrokenException("Duration must be greater than zero.");

            if (bufferBeforeMinutes < 0 || bufferAfterMinutes < 0)
                throw new BusinessRuleBrokenException("Buffers cannot be negative.");
        }

        DurationMinutes = durationMinutes;
        BufferBeforeMinutes = bufferBeforeMinutes;
        BufferAfterMinutes = bufferAfterMinutes;
    }
}
