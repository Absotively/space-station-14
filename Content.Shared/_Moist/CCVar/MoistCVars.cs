using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Moist.CCVar;

[CVarDefs]
public sealed partial class MoistCVars : CVars
{
    public static readonly CVarDef<bool> MoistPlaySelfSpeechSounds =
        CVarDef.Create("moist.play_self_speech_sounds", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> MoistPlaySelfEmotes =
        CVarDef.Create("moist.play_self_emotes", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<string> MoistMutedSpeechSounds =
        CVarDef.Create("moist.muted_speech_sounds", "", CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<string> MoistMutedEmoteSpecies =
        CVarDef.Create("moist.muted_emote_species", "", CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);
}
