using Robust.Shared.Console;

namespace Content.Client._FarHorizons.LightGrid.Commands;

public sealed class ShowLightCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _e = default!;

    public string Command => "showlight";
    public string Description => "Toggles the light grid debug overlay";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var enabled = _e.System<LightDebugOverlaySystem>().Toggle();
        shell.WriteLine(enabled
            ? "Enabled the light grid debug overlay"
            : "Disabled the light grid debug overlay");
    }
}