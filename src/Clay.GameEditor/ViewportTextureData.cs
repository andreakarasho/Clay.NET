namespace Clay.GameEditor;

/// <summary>
/// Custom render data that carries an opaque reference to whatever the viewport rendered.
/// The renderer casts this to the appropriate raylib type.
/// </summary>
public class ViewportTextureData
{
    public object RenderTexture = null!;
}
