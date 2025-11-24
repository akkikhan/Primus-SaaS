namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Options controlling safe serialization for log state.
/// </summary>
public class SerializationOptions
{
    /// <summary>
    /// Maximum depth to traverse when sanitizing complex objects.
    /// </summary>
    public int MaxDepth { get; set; } = 5;

    /// <summary>
    /// Maximum number of items to capture from an enumerable.
    /// </summary>
    public int MaxEnumerableLength { get; set; } = 25;

    /// <summary>
    /// Maximum length of any captured string value.
    /// </summary>
    public int MaxStringLength { get; set; } = 4096;

    /// <summary>
    /// Maximum serialized payload size for a log entry (approximate, in bytes).
    /// When exceeded, context is truncated to a minimal summary.
    /// </summary>
    public int MaxContextBytes { get; set; } = 10 * 1024;

    /// <summary>
    /// Whether to ignore cycles during serialization.
    /// </summary>
    public bool IgnoreCycles { get; set; } = true;

    internal SerializationOptions Clone()
    {
        return new SerializationOptions
        {
            MaxDepth = MaxDepth,
            MaxEnumerableLength = MaxEnumerableLength,
            MaxStringLength = MaxStringLength,
            MaxContextBytes = MaxContextBytes,
            IgnoreCycles = IgnoreCycles
        };
    }

    internal void Validate()
    {
        if (MaxDepth <= 0) throw new ArgumentOutOfRangeException(nameof(MaxDepth), "MaxDepth must be greater than zero.");
        if (MaxEnumerableLength <= 0) throw new ArgumentOutOfRangeException(nameof(MaxEnumerableLength), "MaxEnumerableLength must be greater than zero.");
        if (MaxStringLength <= 0) throw new ArgumentOutOfRangeException(nameof(MaxStringLength), "MaxStringLength must be greater than zero.");
        if (MaxContextBytes <= 512) throw new ArgumentOutOfRangeException(nameof(MaxContextBytes), "MaxContextBytes must be greater than 512 bytes.");
    }
}
