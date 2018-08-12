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
        public bool hasCities;
        public bool hasWater;

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

        public Tileset tileset;
    }
}
