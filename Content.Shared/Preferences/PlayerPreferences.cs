using System.Linq;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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

        public PlayerPreferences(
            IEnumerable<KeyValuePair<int, ICharacterProfile>> characters,
            int selectedCharacterIndex,
            Color adminOOCColor,
            String? highestPriorityJob = null)
        {
            _characters = new Dictionary<int, ICharacterProfile>(characters);
            SelectedCharacterIndex = selectedCharacterIndex;
            AdminOOCColor = adminOOCColor;
        }

        /// <summary>
        ///     All player characters.
        /// </summary>
        public IReadOnlyDictionary<int, ICharacterProfile> Characters => _characters;

        public ICharacterProfile GetProfile(int index)
        {
            return _characters[index];
        }

        /// <summary>
        ///     Index of the currently selected character.
        /// </summary>
        public int SelectedCharacterIndex { get; }

        /// <summary>
        ///     The currently selected character. Always used for the character editor.
        ///     Used for round start if <see cref="CCVars.MultipleCharacterSelection"/>
        ///     is false.
        /// </summary>
        public ICharacterProfile SelectedCharacter => Characters[SelectedCharacterIndex];

        /// <summary>
        ///     Only used if <see cref="CCVars.MultipleCharacterSelection"/> is true.
        /// </summary>
        public IReadOnlyList<ICharacterProfile> RoundStartCandidates => new List<ICharacterProfile>(Characters.Values.Where(c => c.RoundStartCandidate));

        public Color AdminOOCColor { get; set; }

        /// <summary>
        /// Used for round start job assignment when <see cref="CCVars.Game.MultipleCharacterSelection"/>
        /// is true.
        /// </summary>
        public String? HighestPriorityJob { get; set; }

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
