using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.World.Generation
{
    public struct GeneratorParameter
    {
        public Point size;
        public int baseHeight;
        public int minHeight;
        public int maxHeight;
        /// <summary>
        /// minHeight - waterMinDiff is the height that is water. This allows to have more flat grounds without it all being water.
        /// </summary>
        public int waterMinDiff;
        public bool hasCities;
        public bool hasWater;
        public bool hasRivers;
        public bool hasCityConnections;

        /// <summary>
        /// float between 0.0 and 1.0 describing the amount of forest on the map
        /// </summary>
        public float forestSize;

        /// <summary>
        /// float between 0.0 and 1.0 describing the number of cities on the map
        /// </summary>
        public float citiesNumber;

        /// <summary>
        /// float between 0.0 and 1.0 describing the average size of cities.
        /// </summary>
        public float citySize;

        public float citySizeRandomOffset;

        public int randomSeed;

        public Tileset tileset;
    }
}
