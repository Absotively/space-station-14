using System.Linq;
using Content.Shared._Moist.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Speech;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moist.Mobs;

public sealed class MuteableMobSoundsSystem : EntitySystem
{
    // Muteable mob types that aren't round start species.
    // Round start species are automatically muteable.
    private static readonly List<string> MuteableMobTypesExtra =
    [
        "Borg",
        "Scurret",
    ];

    [Dependency] private readonly INetConfigurationManager _netConfigMan = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private HashSet<string> _mutedSpeechSoundsIDs { get; set; } = [];
    private HashSet<string> _mutedEmoteSpeciesIDs { get; set; } = [];

    private EntityQuery<HumanoidAppearanceComponent> _humanoidAppearanceQuery;

    public struct MuteableMobType
    {
        public string Name;
        public bool voiceMuteable; // False if it uses the default voice, we don't want a checkbox then
        public bool voiceMuted;
        public string speechSoundsProtoId;
        public bool emotesMuteable;
        public bool emotesMuted;
        public string mobProtoId;
    }

    private List<MuteableMobType> _mobTypes = [];

    public override void Initialize()
    {
        _humanoidAppearanceQuery = GetEntityQuery<HumanoidAppearanceComponent>();

        _mutedSpeechSoundsIDs =
            new HashSet<string>(_netConfigMan.GetCVar(MoistCVars.MoistMutedSpeechSounds).Split(","));

        _mutedEmoteSpeciesIDs =
            new HashSet<string>(_netConfigMan.GetCVar(MoistCVars.MoistMutedEmoteSpecies).Split(","));

        // We only want to bother with these if the exist and at least one thing uses them
        const string speciesBaseId = "BaseSpeciesMob";
        _protoMan.Resolve(speciesBaseId, out var speciesBasePrototype);
        ProtoId<SpeechSoundsPrototype>? speciesBaseVoiceId = null;
        if (speciesBasePrototype?.TryGetComponent<SpeechComponent>("Speech", out var speciesBaseSpeechComponent) ?? false)
        {
            speciesBaseVoiceId = speciesBaseSpeechComponent.SpeechSounds;
        }
        var genericVoicesMutable = false;

        List<SpeciesPrototype> roundStartSpecies = _protoMan.EnumeratePrototypes<SpeciesPrototype>().Where(o => o.RoundStart).ToList();
        roundStartSpecies.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        foreach (var species in roundStartSpecies)
        {
            if (!_protoMan.Resolve(species.Prototype, out var speciesEntityPrototype))
                continue;

            var mobType = new MuteableMobType
            {
                Name = species.Name,
                voiceMuteable = true,
                speechSoundsProtoId = "",
                emotesMuteable = true,
                emotesMuted = _mutedEmoteSpeciesIDs.Contains(speciesEntityPrototype.ID),
                mobProtoId = speciesEntityPrototype.ID,
            };

            if (speciesEntityPrototype.TryGetComponent<SpeechComponent>("Speech", out var speechComponent))
            {
                mobType.speechSoundsProtoId = speechComponent.SpeechSounds ?? "";
                if (speciesBaseVoiceId != null && mobType.speechSoundsProtoId == speciesBaseVoiceId)
                {
                    mobType.voiceMuteable = false;
                    genericVoicesMutable = true;
                }

                mobType.voiceMuted = _mutedSpeechSoundsIDs.Contains(mobType.speechSoundsProtoId);
            }

            _mobTypes.Add(mobType);
        }

        foreach (var id in MuteableMobTypesExtra)
        {
            if (_protoMan.Resolve(id, out var prototype))
            {
                var mobType = new MuteableMobType
                {
                    Name = prototype.Name,
                    voiceMuteable = true,
                    speechSoundsProtoId = "",
                    emotesMuteable = true,
                    emotesMuted = _mutedEmoteSpeciesIDs.Contains(prototype.ID),
                    mobProtoId = prototype.ID,
                };
                if (prototype.TryGetComponent<SpeechComponent>("Speech", out var speechComponent))
                {
                    mobType.speechSoundsProtoId = speechComponent.SpeechSounds ?? "";
                    if (speciesBaseVoiceId != null && mobType.speechSoundsProtoId == speciesBaseVoiceId)
                    {
                        mobType.voiceMuteable = false;
                        genericVoicesMutable = true;
                    }

                    mobType.voiceMuted = _mutedSpeechSoundsIDs.Contains(mobType.speechSoundsProtoId);
                }

                _mobTypes.Add(mobType);
            }
        }

        if (genericVoicesMutable)
        {
            var genericMobType = new MuteableMobType
            {
                Name = Loc.GetString("moist-ui-audio-generic-sounds-label"),
                voiceMuteable = true,
                speechSoundsProtoId = speciesBaseVoiceId!,
                emotesMuteable = false,
                emotesMuted = false,
                mobProtoId = "",
            };
            _mobTypes.Insert(0, genericMobType);
        }
    }

    public void SetVoiceMuted(string voiceID, bool muted)
    {
        for (int i = 0; i < _mobTypes.Count; i++)
        {
            if (_mobTypes[i].speechSoundsProtoId == voiceID)
            {
                var muteableMobType = _mobTypes[i];
                muteableMobType.voiceMuted = muted;
                _mobTypes[i] = muteableMobType;
            }
        }

        if (muted)
        {
            _mutedSpeechSoundsIDs.Add(voiceID);
        }
        else
        {
            _mutedSpeechSoundsIDs.Remove(voiceID);
        }
        _netConfigMan.SetCVar(MoistCVars.MoistMutedSpeechSounds, string.Join(",",_mutedSpeechSoundsIDs));
    }

    public void SetEmotesMuted(string speciesID, bool muted)
    {
        for (int i = 0; i < _mobTypes.Count; i++)
        {
            if (_mobTypes[i].mobProtoId == speciesID)
            {
                var muteableMobType = _mobTypes[i];
                muteableMobType.emotesMuted = muted;
                _mobTypes[i] = muteableMobType;
            }
        }

        if (muted)
        {
            _mutedEmoteSpeciesIDs.Add(speciesID);
        }
        else
        {
            _mutedEmoteSpeciesIDs.Remove(speciesID);
        }
        _netConfigMan.SetCVar(MoistCVars.MoistMutedEmoteSpecies, string.Join(",",_mutedEmoteSpeciesIDs));
    }
};
