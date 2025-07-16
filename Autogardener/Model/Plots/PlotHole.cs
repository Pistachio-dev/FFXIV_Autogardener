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

        public uint DesignatedPlant { get; set; } //ItemId
        public uint DesignatedSoil { get; set; } //ItemId

        public uint CurrentPlant { get; set; } //ItemId

        public DateTime? LastTendedUtc { get; set; } // That the plugin knows of. I it gets tended without the plugin, it won't know. But you can't overtend so, great.
        public DateTime? LastFertilized { get; set; } // Same as above
        public bool DoNotHarvest { get; set; } // For those plants that you leave up, for interbreeding
        public ulong GameObjectId { get; set; }
        public uint EntityId { get; set; }
        public uint ObjectIndex { get; set; }

        public uint DataId { get; set; }

        public Vector3 Location { get; set; }
    }
}
