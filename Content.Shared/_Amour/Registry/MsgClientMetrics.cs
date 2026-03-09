using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Amour.Registry;

public sealed class MsgClientMetrics : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public List<string> MetricTokens { get; set; } = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadInt32();
        MetricTokens = new List<string>(count);
        for (var i = 0; i < count; i++)
            MetricTokens.Add(buffer.ReadString());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(MetricTokens.Count);
        foreach (var token in MetricTokens)
            buffer.Write(token);
    }
}
