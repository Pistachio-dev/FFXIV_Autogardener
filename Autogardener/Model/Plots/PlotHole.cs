using Autogardener.Model.Designs;
using Dalamud.Game.ClientState.Objects.Types;

namespace Autogardener.Model.Plots
{
    public class PlotHole
    {
        public PlotHole(ulong gameObjectId, uint entityId, uint objectIndex, uint dataId, Vector3 location)
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
            Location = ob.Position;
        }

        public PlotHolePlan? Design { get; set; } = null;

        public uint CurrentPlant { get; set; } //ItemId

        public DateTime? LastTendedUtc { get; set; } // That the plugin knows of. I it gets tended without the plugin, it won't know. But you can't overtend so, great.
        public DateTime? LastFertilized { get; set; } // Same as above
        public ulong GameObjectId { get; set; }
        public uint EntityId { get; set; }
        public uint ObjectIndex { get; set; }

        public uint DataId { get; set; }

        public Vector3 Location { get; set; }
    }
}
