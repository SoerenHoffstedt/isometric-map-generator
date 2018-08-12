using Industry.Simulation;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.World.Generation
{
    public static class CityGenerator
    {
        static float MIN_CITY_SIZE = 100;
        static float MAX_CITY_SIZE = 900;
        static int   LENGTH_HIGH_ROAD = 8;
        static int   DELTA_HIGH_ROAD = 3;
        static int   LENGTH_LOW_ROAD = 3;
        static int   DELTA_LOW_ROAD = 1;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="tileset"></param>
        /// <param name="citySize">Size of city betwenn 0 and 1</param>
        public static void GrowCity(Tile[,] tiles,Tileset tileset, Point startPosition, float citySize)
        {
            Debug.Assert(citySize >= 0 && citySize <= 1);
            int citySizeInTiles = (int)Math.Round(MIN_CITY_SIZE + citySize * (MAX_CITY_SIZE - MIN_CITY_SIZE));

        }

    }


    class CityTile
    {
        public RoadType road = RoadType.None;
        public CitizenLevel level = CitizenLevel.None;
    }

    enum RoadType
    {
        None,
        Low,
        High
    }

}
