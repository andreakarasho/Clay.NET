namespace Clay;

/// <summary>
/// Interface for rendering Clay UI elements.
/// Implement this interface to create a custom renderer for your graphics backend.
/// </summary>
public interface IClayRenderer
{
    /// <summary>
    /// Render a batch of commands from the layout computation.
    /// Commands are already sorted by z-index in ascending order.
    /// </summary>
    /// <param name="commands">The render commands to execute.</param>
    void Render(ReadOnlySpan<RenderCommand> commands);
}

/// <summary>
/// Extension methods for working with render commands.
/// </summary>
public static class RenderCommandExtensions
{
    /// <summary>
    /// Helper to iterate through render commands with a delegate.
    /// </summary>
    public static void ForEach(this ReadOnlySpan<RenderCommand> commands, Action<RenderCommand> action)
    {
        for (int i = 0; i < commands.Length; i++)
        {
            action(commands[i]);
        }
    }

    /// <summary>
    /// Helper to iterate through render commands with index.
    /// </summary>
    public static void ForEach(this ReadOnlySpan<RenderCommand> commands, Action<RenderCommand, int> action)
    {
        for (int i = 0; i < commands.Length; i++)
        {
            action(commands[i], i);
        }
    }
}
