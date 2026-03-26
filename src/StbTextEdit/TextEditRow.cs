namespace StbTextEdit;

/// <summary>
/// Result of a row layout query. Describes the shape of one displayed row of characters.
/// </summary>
public struct TextEditRow
{
    /// <summary>Starting x location of the row.</summary>
    public float X0;

    /// <summary>Ending x location of the row (allows for alignment).</summary>
    public float X1;

    /// <summary>Position of baseline relative to previous row's baseline.</summary>
    public float BaselineYDelta;

    /// <summary>Top of row relative to baseline.</summary>
    public float YMin;

    /// <summary>Bottom of row relative to baseline.</summary>
    public float YMax;

    /// <summary>Number of characters consumed by this row (including trailing newline).</summary>
    public int NumChars;
}
