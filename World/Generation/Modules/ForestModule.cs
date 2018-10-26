using Barely.ProgGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.World.Generation.Modules
{
    public class ForestModule : IGeneratorModule
    {
        private const int BLOCKING_PERC_MIN = 48;
        private const int BLOCKING_PERC_MAX = 55;
        private const int SMOOTH_ITERATIONS = 3;

        public void Apply(GeneratorParameter param, Tile[,] tiles)
        {
            if (param.forestSize <= 0f)
                return;
            
            float paramValue = 1 - param.forestSize;
            int perc = (int)(BLOCKING_PERC_MIN + (BLOCKING_PERC_MAX - BLOCKING_PERC_MIN) * paramValue);
            bool[,] automata = CellularAutomata.Generate(param.size, SMOOTH_ITERATIONS, perc, true, param.randomSeed);

            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    Tile t = tiles[x, y];
                    if (!automata[x, y] && t.type == TileType.Nothing)
                    {
                        t.type = TileType.Forest;
                        t.onTopIndex = param.tileset.GetRandomIndex(TileType.Forest);
                    }
                }
            }
        }
    }
}
