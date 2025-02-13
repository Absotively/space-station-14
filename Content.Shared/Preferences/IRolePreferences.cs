using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences
{
    public interface IRolePreferences
    {
        public IReadOnlyDictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities { get; }

        public IReadOnlySet<ProtoId<AntagPrototype>> AntagPreferences { get; }

        public PreferenceUnavailableMode PreferenceUnavailable { get; }

        bool MemberwiseEquals(IRolePreferences other);

        /// <summary>
        ///     Makes this profile valid so there's no bad data like negative ages.
        /// </summary>
        void EnsureValid(IDependencyCollection collection);

        /// <summary>
        /// Gets a copy of this profile that has <see cref="EnsureValid"/> applied, i.e. no invalid data.
        /// </summary>
        IRolePreferences Validated(IDependencyCollection collection);
    }
}
