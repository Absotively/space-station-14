using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// The client sends this to update role preferences.
    /// </summary>
    public sealed class MsgUpdateRolePreferences : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public int Slot;
        public IRolePreferences RolePreferences = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            Slot = buffer.ReadInt32();
            var length = buffer.ReadVariableInt32();
            using var stream = new MemoryStream(length);
            buffer.ReadAlignedMemory(stream, length);
            RolePreferences = serializer.Deserialize<IRolePreferences>(stream);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(Slot);
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, RolePreferences);
                buffer.WriteVariableInt32((int) stream.Length);
                stream.TryGetBuffer(out var segment);
                buffer.Write(segment);
            }
        }
    }
}
