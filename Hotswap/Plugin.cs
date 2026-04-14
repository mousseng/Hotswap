using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;

namespace Hotswap;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] private static ICommandManager CommandManager { get; set; } = null!;
    [PluginService] private static IChatGui Chat { get; set; } = null!;
    [PluginService] private static IPluginLog Log { get; set; } = null!;

    public Plugin()
    {
        CommandManager.AddHandler("/hotswap", new CommandInfo(DoSwap));
    }

    public void Dispose()
    {
        CommandManager.RemoveHandler("/hotswap");
    }

    private static unsafe void DoSwap(string command, string arguments)
    {
        var args = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length != 3)
        {
            Log.Warning("3 args required: macro id, hotbar id, slot id");
            Chat.Print("[hotswap] 3 args required: macro id, hotbar id, slot id");
            return;
        }

        try
        {
            var macroId = uint.Parse(args[0]);
            var hotbarId = uint.Parse(args[1]) - 1;
            var slotId = uint.Parse(args[2]) - 1;

            // individual macros run from 0 to 255, and shared macros run
            // from 256 to 512. effectively, the game uses the 9th bit to
            // indicate which "set" a macro belongs to.
            var adjustedMacroId = macroId < 100 ? macroId : macroId % 100 + 256;

            // normally we should just use SetAndSave() to do this, but for some
            // reason it doesn't work. so we have to manually set the slot and
            // flush it to the save file.
            var hotbars = RaptureHotbarModule.Instance();
            var slots = hotbars->Hotbars[(int)hotbarId].Slots;
            slots[(int)slotId].Set(RaptureHotbarModule.HotbarSlotType.Macro, adjustedMacroId);

            hotbars->WriteSavedSlot(
                hotbars->ActiveHotbarClassJobId,
                hotbarId,
                slotId,
                slots.GetPointer((int)slotId),
                false,
                false);
        }
        catch (Exception e)
        {
            Log.Error(e, "Ran into an error");
            Chat.Print("[hotswap] Ran into an error");
        }
    }
}
