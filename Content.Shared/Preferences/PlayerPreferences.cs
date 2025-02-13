using Content.Shared.CCVar;
using System.Linq;
using System.Xml.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Collections.Generic;

namespace Content.Shared.Preferences
{
    /// <summary>
    ///     Contains all player characters and the index of the currently selected character.
    ///     Serialized both over the network and to disk.
    /// </summary>
    [Serializable]
    [NetSerializable]
    public sealed class PlayerPreferences
    {
        private Dictionary<int, ICharacterProfile> _characters;

        private RolePreferences _rolePreferences;

        public PlayerPreferences(
            IEnumerable<KeyValuePair<int, ICharacterProfile>> characters,
            int selectedCharacterIndex,
            Color adminOOCColor,
            IRolePreferences rolePrefs)
        {
            _characters = new Dictionary<int, ICharacterProfile>(characters);
            SelectedCharacterIndex = selectedCharacterIndex;
            AdminOOCColor = adminOOCColor;
            _rolePreferences = (RolePreferences)rolePrefs;
        }

        public PlayerPreferences(PlayerPreferences other)
            : this(new Dictionary<int, ICharacterProfile>(other.Characters),
                other.SelectedCharacterIndex,
                other.AdminOOCColor,
                other.RolePreferences)
        {
        }

        /// <summary>
        ///     All player characters.
        /// </summary>
        public IReadOnlyDictionary<int, ICharacterProfile> Characters => _characters;

        /// <summary>
        /// Role preferences for initial spawn and also mid-round antag selection.
        /// </summary>
        [DataField]
        public RolePreferences RolePreferences => _rolePreferences;

        public ICharacterProfile GetProfile(int index)
        {
            return _characters[index];
        }

        /// <summary>
        ///     Index of the currently selected character.
        /// </summary>
        public int SelectedCharacterIndex { get; }

        /// <summary>
        ///     The currently selected character.
        /// </summary>
        public ICharacterProfile SelectedCharacter => Characters[SelectedCharacterIndex];

        public Color AdminOOCColor { get; set; }

        public int IndexOfCharacter(ICharacterProfile profile)
        {
            return _characters.FirstOrNull(p => p.Value == profile)?.Key ?? -1;
        }

        public bool TryIndexOfCharacter(ICharacterProfile profile, out int index)
        {
            return (index = IndexOfCharacter(profile)) != -1;
        }
    }
}
