using Assets.Habitat;
using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.PreComputation.Configurations
{
    public class HabitatMapDbPrecomputationConfiguration
    {
        private PrecomputationConfiguration _precomputationConfiguration;

        public MyRectangle AreaOnMap =>
            //_precomputationConfiguration.Repositioner.InvMove(new UnityCoordsPositions2D(-360, -360, 1080, 1080));
            _precomputationConfiguration.Repositioner.InvMove(
                MyRectangle.FromVertex(new Vector2(-3600*3, -3600*3), new Vector2(3600*3, 3600*3)));
        //new UnityCoordsPositions2D(
        //    56 * 720, 59 * 720, (8*3) * 720, (8*3) * 720);

        public Vector2 MapGridSize = new Vector2(90 * 4, 90 * 4);
        public HabitatType DefaultHabitatType = HabitatType.NotSpecified;
        public HabitatTypePriorityResolver HabitatTypePriorityResolver = HabitatTypePriorityResolver.Default;

        public HabitatMapDbPrecomputationConfiguration(PrecomputationConfiguration precomputationConfiguration)
        {
            _precomputationConfiguration = precomputationConfiguration;
        }
    }
}