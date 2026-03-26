using Clay;

namespace Clay.Test;

/// <summary>
/// Helper to safely initialize/shutdown Clay for each test.
/// </summary>
public sealed class ClayFixture : IDisposable
{
    public Dimensions LayoutSize { get; }

    public ClayFixture(float width = 800, float height = 600)
    {
        LayoutSize = new Dimensions(width, height);
        ClayApi.Initialize(LayoutSize, new SimpleTextMeasurer(), maxElementCount: 8192);
    }

    public void Dispose()
    {
        ClayApi.Shutdown();
    }
}
