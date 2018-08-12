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
        public void Apply(GeneratorParameter param, Tile[,] tiles)
        {
            if (param.forestSize <= 0f)
                return;

            bool[,] automata = CellularAutomata.Generate(param.size, 4, 50, true);

            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    Tile t = tiles[x, y];
                    if (!automata[x, y] && t.type == TileType.Nothing && t.AllHeightsAreSame())
                    {
                        t.type = TileType.Forest;
                        t.onTopIndex = param.tileset.GetRandomIndex(TileType.Forest);
                    }
                }
            }
        }
    }
}
