using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// Role preferences data. Like HumanoidCharacterProfile, this Looks immutable, but uses
    /// non-immutable semantics internally for serialization/code sanity purposes.
    ///
    /// Separate from PlayerPreferences so it can be serialized by itself for updates and import/export.
    /// </summary>
    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class RolePreferences : IRolePreferences
    {

        /// <summary>
        /// Job preferences for initial spawn.
        /// </summary>
        [DataField]
        private Dictionary<ProtoId<JobPrototype>, JobPriority> _jobPriorities = new()
        {
            {
                SharedGameTicker.FallbackOverflowJob, JobPriority.High
            }
        };

        /// <summary>
        /// Antags we have opted in to.
        /// </summary>
        [DataField]
        private HashSet<ProtoId<AntagPrototype>> _antagPreferences = new();

        /// <summary>
        /// <see cref="_jobPriorities"/>
        /// </summary>
        public IReadOnlyDictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities => _jobPriorities;

        /// <summary>
        /// <see cref="_antagPreferences"/>
        /// </summary>
        public IReadOnlySet<ProtoId<AntagPrototype>> AntagPreferences => _antagPreferences;

        /// <summary>
        /// If we're unable to get one of our preferred jobs do we spawn as a fallback job or do we stay in lobby.
        /// </summary>
        [DataField]
        public PreferenceUnavailableMode PreferenceUnavailable { get; private set; } =
            PreferenceUnavailableMode.SpawnAsOverflow;

        public RolePreferences(
            IReadOnlyDictionary<ProtoId<JobPrototype>, JobPriority>? jobPriorities = null,
            IReadOnlySet<ProtoId<AntagPrototype>>? antagPreferences = null,
            PreferenceUnavailableMode preferenceUnavailable = PreferenceUnavailableMode.SpawnAsOverflow)
        {
            if (jobPriorities == null)
            {
                _jobPriorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
            }
            else
            {
                _jobPriorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>(jobPriorities);
            }
            PreferenceUnavailable = preferenceUnavailable;
            if (antagPreferences == null)
            {
                _antagPreferences = new HashSet<ProtoId<AntagPrototype>>();
            }
            else
            {
                _antagPreferences = new HashSet<ProtoId<AntagPrototype>>(antagPreferences);
            }
        }

        /// <summary>Copy constructor</summary>
        public RolePreferences(RolePreferences other)
            : this(other.JobPriorities,
                  other.AntagPreferences,
                  other.PreferenceUnavailable)
        {
        }

        public void EnsureValid(IDependencyCollection collection)
        {
            var prototypeManager = collection.Resolve<IPrototypeManager>();

            var prefsUnavailableMode = PreferenceUnavailable switch
            {
                PreferenceUnavailableMode.StayInLobby => PreferenceUnavailableMode.StayInLobby,
                PreferenceUnavailableMode.SpawnAsOverflow => PreferenceUnavailableMode.SpawnAsOverflow,
                _ => PreferenceUnavailableMode.StayInLobby // Invalid enum values.
            };

            EnsurePrioritiesValid(_jobPriorities);

            var antags = AntagPreferences
                .Where(id => prototypeManager.TryIndex(id, out var antag) && antag.SetPreference)
                .ToList();

            PreferenceUnavailable = prefsUnavailableMode;

            _antagPreferences.Clear();
            _antagPreferences.UnionWith(antags);
        }

        private static void EnsurePrioritiesValid(Dictionary<ProtoId<JobPrototype>, JobPriority> priorities)
        {
            var hasHighPrority = false;
            foreach (var (key, value) in priorities)
            {
                if (value == JobPriority.Never)
                    priorities.Remove(key);
                else if (value != JobPriority.High)
                    continue;

                if (hasHighPrority)
                    priorities[key] = JobPriority.Medium;

                hasHighPrority = true;
            }
        }

        public bool MemberwiseEquals(IRolePreferences maybeOther)
        {
            if (maybeOther is not RolePreferences other) return false;
            if (PreferenceUnavailable != other.PreferenceUnavailable) return false;
            if (!_jobPriorities.SequenceEqual(other._jobPriorities)) return false;
            return _antagPreferences.SequenceEqual(other._antagPreferences);
        }

        public IRolePreferences Validated(IDependencyCollection collection)
        {
            var rolePrefs = new RolePreferences(this);
            rolePrefs.EnsureValid(collection);
            return rolePrefs;
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is RolePreferences other && Equals(other);
        }

        public RolePreferences Clone()
        {
            return new RolePreferences(this);
        }

        public RolePreferences WithJobPriorities(IEnumerable<KeyValuePair<ProtoId<JobPrototype>, JobPriority>> jobPriorities)
        {
            var dictionary = new Dictionary<ProtoId<JobPrototype>, JobPriority>(jobPriorities);
            EnsurePrioritiesValid(dictionary);

            return new(this)
            {
                _jobPriorities = dictionary
            };
        }

        public RolePreferences WithJobPriority(ProtoId<JobPrototype> jobId, JobPriority priority)
        {
            var dictionary = new Dictionary<ProtoId<JobPrototype>, JobPriority>(_jobPriorities);
            if (priority == JobPriority.Never)
            {
                dictionary.Remove(jobId);
            }
            else if (priority == JobPriority.High)
            {
                // There can only ever be one high priority job.
                foreach (var (job, value) in dictionary)
                {
                    if (value == JobPriority.High)
                        dictionary[job] = JobPriority.Medium;
                }

                dictionary[jobId] = priority;
            }
            else
            {
                dictionary[jobId] = priority;
            }

            return new(this)
            {
                _jobPriorities = dictionary,
            };
        }

        public RolePreferences WithPreferenceUnavailable(PreferenceUnavailableMode mode)
        {
            return new(this) { PreferenceUnavailable = mode };
        }

        public RolePreferences WithAntagPreferences(IEnumerable<ProtoId<AntagPrototype>> antagPreferences)
        {
            return new(this)
            {
                _antagPreferences = new(antagPreferences),
            };
        }

        public RolePreferences WithAntagPreference(ProtoId<AntagPrototype> antagId, bool pref)
        {
            var list = new HashSet<ProtoId<AntagPrototype>>(_antagPreferences);
            if (pref)
            {
                list.Add(antagId);
            }
            else
            {
                list.Remove(antagId);
            }

            return new(this)
            {
                _antagPreferences = list,
            };
        }
    }
}
