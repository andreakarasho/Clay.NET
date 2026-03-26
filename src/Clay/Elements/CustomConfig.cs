namespace Clay;

/// <summary>
/// Configuration for custom elements (passed through to renderer).
/// </summary>
public struct CustomConfig
{
    /// <summary>
    /// Custom data (passed through to renderer).
    /// </summary>
    public object? CustomData;

    /// <summary>
    /// Creates a custom config with the specified data.
    /// </summary>
    public static CustomConfig Create(object? customData)
        => new() { CustomData = customData };

    /// <summary>
    /// Returns true if this config has custom data.
    /// </summary>
    public readonly bool HasCustomData => CustomData != null;
}
