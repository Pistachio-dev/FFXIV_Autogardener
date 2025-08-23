using Autogardener.Model.Designs;
using Dalamud.Game.ClientState.Objects.Types;

namespace Autogardener.Model.Plots
{
    public class Plot
    {
        public Plot(ulong gameObjectId, uint entityId, uint objectIndex, uint dataId, SerializableVector3 location)
        {
            GameObjectId = gameObjectId;
            EntityId = entityId;
            ObjectIndex = objectIndex;
            DataId = dataId;
            Location = location;
        }

        public void Initialize(IGameObject ob)
        {
            GameObjectId = ob.GameObjectId;
            EntityId = ob.EntityId;
            ObjectIndex = ob.ObjectIndex;
            DataId = ob.DataId;
            Location = new SerializableVector3(ob.Position.X, ob.Position.Y, ob.Position.Z);
        }

        public uint CurrentSeed { get; set; } //ItemId

        public uint CurrentSoil { get; set; }

        public DateTime? LastTendedUtc { get; set; } // That the plugin knows of. I it gets tended without the plugin, it won't know. But you can't overtend so, great.
        public DateTime? LastFertilizedUtc { get; set; } // Same as above
        public ulong GameObjectId { get; set; }
        public uint EntityId { get; set; }
        public uint ObjectIndex { get; set; }

        public uint DataId { get; set; }

        public SerializableVector3 Location { get; set; }
    }
}
