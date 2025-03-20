using Content.Shared.Roles;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    /// <summary>
    /// The client sends this to delete a character profile.
    /// </summary>
    public sealed class MsgSelectHighestPrioJob : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public string? JobID;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            JobID = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(JobID);
        }
    }
}
