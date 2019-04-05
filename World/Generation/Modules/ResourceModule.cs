using Barely.ProgGen;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.World.Generation.Modules
{
    /// <summary>
    /// Places Stone, Oil resources for now. Forest still in separate module.
    /// </summary>
    public class ResourceModule : IGeneratorModule
    {
        private Random random;

        public ResourceModule(Random random)
        {
            this.random = random;
        }

        public void Apply(GeneratorParameter param, Tile[,] tiles)
        {
            if (param.resourceSize == 0.0f)
                return;

            CreateStones(param, tiles);
        }

        private const int BLOCKING_PERC = 55;
        private const int BLOCKING_PERC_DELTA = 5;
        private const int SMOOTH_ITERATIONS = 5;

        private const int MIN_RES_ROOMS = 10;

        private const float STONE_PERCENTAGE    = 0.50f;
        private const float COAL_PERCENTAGE     = 0.20f;
        private const float ORE_PERCENTAGE      = 0.20f;
        private const float OIL_PERCENTAGE      = 0.10f;

        private void CreateStones(GeneratorParameter param, Tile[,] tiles)
        {
            float[,] heatMap = CreateStoneHeatMap(param.size, tiles);

            List<Room> rooms = null;

            while (rooms == null || rooms.Count < MIN_RES_ROOMS)
            {

                int block = (int)(BLOCKING_PERC - BLOCKING_PERC_DELTA / 2 + ((1 - param.resourceSize) * BLOCKING_PERC_DELTA));

                bool[,] automata = CellularAutomata.Generate(param.size, SMOOTH_ITERATIONS, block, false, random.Next());

                for (int y = 0; y < param.size.Y; y++)
                {
                    for (int x = 0; x < param.size.X; x++)
                    {
                        if (automata[x, y] || heatMap[x, y] < 0.5f || !tiles[x, y].AllHeightsAreSame())
                        {
                            automata[x, y] = true;
                        }
                    }
                }

                rooms = GeneratorHelper.GetCellularAutomataAsRooms(automata);                
            }

            rooms.Shuffle(random);

            int coalIndex = (int)(STONE_PERCENTAGE * rooms.Count);
            int oreIndex = (int)((STONE_PERCENTAGE + COAL_PERCENTAGE) * rooms.Count);
            int oilIndex = (int)((STONE_PERCENTAGE + COAL_PERCENTAGE + ORE_PERCENTAGE) * rooms.Count);

            for (int i = 0; i < rooms.Count; ++i)
            {
                TileType res = TileType.Stone;
                if (i >= oilIndex)
                    res = TileType.Oil;
                else if (i >= oreIndex)
                    res = TileType.Ore;
                else if (i >= coalIndex)
                    res = TileType.Coal;

                foreach (Point p in rooms[i].Tiles)
                {
                    if(tiles[p.X, p.Y].type == TileType.Nothing)
                    {
                        tiles[p.X, p.Y].type = res;
                        tiles[p.X, p.Y].onTopIndex = param.tileset.GetRandomIndex(res);
                    }                    
                }                
            }
            
        }

        const float NEAR_PENALTY = 0.25f;
        const int NEAR_PENALTY_DIST = 3;

        /// <summary>
        /// Creates a float heat map that indicates how probable a tile is to contain a resource.
        /// This will be used in conjunction with cellular automata to create resource distribution.
        /// Tiles that are water, cities, roads (not empty in general) are less likely to become resource.
        /// For stone
        /// </summary>
        /// <returns></returns>
        private float[,] CreateStoneHeatMap(Point mapSize, Tile[,] tiles)
        {
            float[,] heatMap = new float[mapSize.X, mapSize.Y];
            
            //init heat map values to 1
            for (int y = 0; y < mapSize.Y; y++)
            {
                for (int x = 0; x < mapSize.X; x++)
                {
                    heatMap[y, x] = 1f;
                }
            }
            

            for (int y = 0; y < mapSize.Y; y++)
            {
                for (int x = 0; x < mapSize.X; x++)
                {
                    if (tiles[x, y].type != TileType.Nothing || x == 0 || y == 0 || x == mapSize.X - 1 || y == mapSize.Y - 1)
                    {
                        heatMap[x,y] = 0f;

                        for (int iy = -NEAR_PENALTY_DIST; iy <= NEAR_PENALTY_DIST; ++iy)
                        {
                            for (int ix = -NEAR_PENALTY_DIST; ix <= NEAR_PENALTY_DIST; ++ix)
                            {
                                if(GeneratorHelper.IsInRange(x + ix, y + iy))
                                    heatMap[x + ix, y + iy] -= NEAR_PENALTY;
                            }
                        }
                    }                                                            
                }
            }

            //cap the heat map values to [0,1]
            /*for (int y = 0; y < mapSize.Y; y++)
            {
                for (int x = 0; x < mapSize.X; x++)
                {
                    if (heatMap[y, x] < 0f)
                        heatMap[y, x] = 0f;
                    else if (heatMap[y, x] > 1f)
                        heatMap[y, x] = 1f;
                }
            }*/

            return heatMap;
        }

    }
}
