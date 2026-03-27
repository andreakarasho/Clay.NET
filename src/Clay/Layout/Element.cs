using System.Numerics;
using System.Runtime.InteropServices;

namespace Clay;

/// <summary>
/// Types of element configurations that can be attached to a layout element.
/// </summary>
public enum ElementConfigType : byte
{
    None = 0,
    Border = 1,
    Floating = 2,
    Scroll = 3,
    Image = 4,
    Text = 5,
    Custom = 6,
    Shared = 7
}

/// <summary>
/// An element configuration entry with type and config index.
/// </summary>
public struct ElementConfig
{
    public ElementConfigType Type;
    public int ConfigIndex;
}

/// <summary>
/// Children data for a layout element.
/// </summary>
public struct LayoutElementChildren
{
    public int StartIndex;
    public int Length;
}

/// <summary>
/// Text element data reference.
/// </summary>
public struct TextElementDataRef
{
    public int Index;
    public bool IsText;
}

/// <summary>
/// Text element data.
/// </summary>
public struct TextElementData
{
    /// <summary>
    /// The text content.
    /// </summary>
    public string Text;

    /// <summary>
    /// Preferred (measured) dimensions of the text.
    /// </summary>
    public Dimensions PreferredDimensions;

    /// <summary>
    /// Index of this element in the layout elements array.
    /// </summary>
    public int ElementIndex;

    /// <summary>
    /// Start index of wrapped lines in the wrapped lines array.
    /// </summary>
    public int WrappedLinesStart;

    /// <summary>
    /// Number of wrapped lines.
    /// </summary>
    public int WrappedLinesLength;
}

/// <summary>
/// Internal representation of a layout element during layout computation.
/// </summary>
public struct LayoutElement
{
    /// <summary>
    /// Children element indices.
    /// </summary>
    public LayoutElementChildren Children;

    /// <summary>
    /// Reference to text data if this is a text element.
    /// </summary>
    public TextElementDataRef TextData;

    /// <summary>
    /// Computed dimensions of this element.
    /// </summary>
    public Dimensions Dimensions;

    /// <summary>
    /// Minimum dimensions (used for compression/shrinking).
    /// </summary>
    public Dimensions MinDimensions;

    /// <summary>
    /// Index of the layout configuration in the configs array.
    /// </summary>
    public int LayoutConfigIndex;

    /// <summary>
    /// Start index of element configurations.
    /// </summary>
    public int ElementConfigsStart;

    /// <summary>
    /// Number of element configurations.
    /// </summary>
    public int ElementConfigsLength;

    /// <summary>
    /// Unique identifier for this element.
    /// </summary>
    public uint Id;

    /// <summary>
    /// Returns true if this is a text element.
    /// </summary>
    public readonly bool IsTextElement => TextData.IsText;
}

/// <summary>
/// A node in the layout element tree during layout computation.
/// </summary>
public struct LayoutElementTreeNode
{
    public int LayoutElementIndex;
    public Vector2 Position;
    public Vector2 NextChildOffset;
}

/// <summary>
/// Root of a layout element tree (for floating elements).
/// </summary>
public struct LayoutElementTreeRoot
{
    public int LayoutElementIndex;
    public uint ParentId;
    public uint ClipElementId;
    public short ZIndex;
    public Vector2 PointerOffset;
}

/// <summary>
/// Hash map item for element lookup by ID.
/// </summary>
public struct LayoutElementHashMapItem
{
    public BoundingBox BoundingBox;
    public BoundingBox ClipBounds;
    public ElementId ElementId;
    public int LayoutElementIndex;
    public int NextIndex;
    public uint Generation;
    public uint IdAlias;
    public bool HasClipBounds;
}

