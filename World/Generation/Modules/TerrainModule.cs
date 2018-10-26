using Barely.ProgGen;
using Microsoft.Xna.Framework;
using static Industry.World.Generation.GenHelper;

namespace Industry.World.Generation.Modules
{
    class TerrainModule : IGeneratorModule
   {
        
        public void Apply(GeneratorParameter param, Tile[,] tiles)
        {
            Noise.PerlinInit();
            float[,] heights = Noise.GetNoise(new Point(512, 512), 5, 2, 2.5f, 0.25f, param.randomSeed);

            System.Func<float, float> HeightFunc = Glide.Ease.QuadInOut;

            int maxHeight = param.maxHeight - 1;
            int waterHeight = param.minHeight - param.waterMinDiff;

            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    double round = HeightFunc(heights[x, y]);
                    int h = (int)(round * (double)maxHeight) + param.baseHeight;

                    tiles[x, y] = new Tile(new Point(x, y), h, h, h, h);

                    if (h < param.minHeight)
                    {
                        int m = param.minHeight;
                        tiles[x, y].height = new int[] { m, m, m, m };
                        if (param.hasWater && h < waterHeight)
                            tiles[x, y].type = TileType.Water;
                    }
                }
            }

            SmoothHeight(param, tiles);

            CreateSlopes(param, tiles);
        }

        private void SmoothHeight(GeneratorParameter param, Tile[,] tiles)
        {
            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    int h = tiles[x, y].GetMaxHeight();
                    int smallerNeighbours = 0;
                    int biggerNeighbours = 0;

                    foreach (Point p in IterateNeighboursEightDir(x, y))
                    {
                        int nh = tiles[p.X, p.Y].GetMaxHeight();
                        if (nh < h)
                            smallerNeighbours++;
                        else if (nh > h)
                            biggerNeighbours++;
                    }

                    if (smallerNeighbours > 5)
                    {
                        h -= 1;
                        tiles[x, y].height = new int[] { h, h, h, h };
                    }
                    else if (biggerNeighbours > 5)
                    {
                        h += 1;
                        tiles[x, y].height = new int[] { h, h, h, h };
                    }

                }
            }
        }

        private void CreateSlopes(GeneratorParameter param, Tile[,] tiles)
        {

            bool AllHeightsHigher(int[] heights, int than)
            {
                for (int i = 0; i < heights.Length; i++)
                {
                    if (heights[i] <= than)
                        return false;
                }
                return true;
            }

            bool HasSlopeUp(SlopeIndex slope, int x, int y)
            {
                int h = tiles[x, y].GetMaxHeight();

                switch (slope)
                {
                    case SlopeIndex.North:
                        if (IsInRange(x, y - 1))
                        {
                            int height = tiles[x, y - 1].GetMaxHeight();

                            return (height == h + 1 || height == h + 2) && AllHeightsHigher(tiles[x, y - 1].height, h);
                        }
                        else
                            return false;
                    case SlopeIndex.East:
                        if (IsInRange(x + 1, y))
                        {
                            int height = tiles[x + 1, y].GetMaxHeight();
                            return (height == h + 1 || height == h + 2) && AllHeightsHigher(tiles[x + 1, y].height, h);
                        }
                        else
                            return false;
                    case SlopeIndex.South:
                        if (IsInRange(x, y + 1))
                        {
                            int height = tiles[x, y + 1].GetMaxHeight();
                            return (height == h + 1 || height == h + 2) && AllHeightsHigher(tiles[x, y + 1].height, h);
                        }
                        else
                            return false;
                    case SlopeIndex.West:
                        if (IsInRange(x - 1, y))
                        {
                            int height = tiles[x - 1, y].GetMaxHeight();
                            return (height == h + 1 || height == h + 2) && AllHeightsHigher(tiles[x - 1, y].height, h);
                        }
                        else
                            return false;
                }

                return false;
            }


            // check for 3 higher neighbours -> make it higher
            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    int h = tiles[x, y].GetMaxHeight();
                    if (!tiles[x, y].AllHeightsAreSame())
                        continue;

                    int higher = 0;
                    foreach (Point p in IterateNeighboursEightDir(x, y))
                    {
                        if (p.X == x || p.Y == y)
                        {
                            if (tiles[p.X, p.Y].GetMaxHeight() > h)
                                higher++;
                        }
                    }

                    if (higher >= 3)
                    {
                        for (int i = 0; i < tiles[x, y].height.Length; i++)
                            tiles[x, y].height[i] += 1;
                    }
                }
            }


            //check for 3 corner slops
            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    /*if(tiles[x, y].type == TileType.Water)
                    {
                        tiles[x, y].height = new int[] { 5, 5, 5, 5 };
                    }*/

                    if (!tiles[x, y].AllHeightsAreSame())
                        continue;

                    int directions = 0;

                    if (HasSlopeUp(SlopeIndex.North, x, y))
                        directions += (int)SlopeIndex.North;

                    if (HasSlopeUp(SlopeIndex.East, x, y))
                        directions += (int)SlopeIndex.East;

                    if (HasSlopeUp(SlopeIndex.South, x, y))
                        directions += (int)SlopeIndex.South;

                    if (HasSlopeUp(SlopeIndex.West, x, y))
                        directions += (int)SlopeIndex.West;


                    if (directions > 0)
                    {
                        if (directions == 3 || directions == 6 || directions == 12 || directions == 9)
                        {
                            int oldH = tiles[x, y].height[0];
                            for (int i = 0; i < 4; i++)
                                tiles[x, y].height[i] += 1;

                            if (directions == 3)
                                tiles[x, y].height[3]--;

                            if (directions == 6)
                                tiles[x, y].height[0]--;

                            if (directions == 12)
                                tiles[x, y].height[1]--;

                            if (directions == 9)
                                tiles[x, y].height[2]--;
                        }
                    }
                }
            }


            //check for ramp slopes
            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {

                    if (!tiles[x, y].AllHeightsAreSame() || tiles[x, y].type == TileType.Water)
                        continue;

                    int directions = 0;


                    if (HasSlopeUp(SlopeIndex.North, x, y))
                    {
                        directions += (int)SlopeIndex.North;
                    }
                    else if (HasSlopeUp(SlopeIndex.East, x, y))
                    {
                        directions += (int)SlopeIndex.East;
                    }
                    else if (HasSlopeUp(SlopeIndex.South, x, y))
                    {

                        directions += (int)SlopeIndex.South;
                    }
                    else if (HasSlopeUp(SlopeIndex.West, x, y))
                    {
                        directions += (int)SlopeIndex.West;
                    }

                    if (directions > 0)
                    {
                        int oldH = tiles[x, y].height[0];
                        for (int i = 0; i < 4; i++)
                            tiles[x, y].height[i] -= 1;



                        if ((directions & 1) == 0)  //north
                        {
                            tiles[x, y].height[2] += 1;
                            tiles[x, y].height[3] += 1;
                        }

                        if ((directions & 2) == 0)
                        {
                            tiles[x, y].height[0] += 1;
                            tiles[x, y].height[3] += 1;
                        }

                        if ((directions & 4) == 0)
                        {
                            tiles[x, y].height[1] += 1;
                            tiles[x, y].height[0] += 1;
                        }

                        if ((directions & 8) == 0)
                        {
                            tiles[x, y].height[2] += 1;
                            tiles[x, y].height[1] += 1;
                        }

                    }

                }
            }


            // check for one-corner slops
            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    int h = tiles[x, y].GetMaxHeight();
                    if (!tiles[x, y].AllHeightsAreSame() || tiles[x, y].type == TileType.Water)
                        continue;

                    //tiles[x, y].color = Color.Red;

                    if (IsInRange(x - 1, y - 1))
                    {
                        int height = tiles[x - 1, y - 1].GetMaxHeight();
                        if (AllHeightsHigher(tiles[x - 1, y - 1].height, h) && (height == h + 1 || height == h + 2))
                        {
                            tiles[x, y].height[0] += 1;
                            continue;
                        }
                    }


                    if (IsInRange(x - 1, y + 1))
                    {
                        int height = tiles[x - 1, y + 1].GetMaxHeight();
                        if (AllHeightsHigher(tiles[x - 1, y + 1].height, h) && (height == h + 1 || height == h + 2))
                        {
                            tiles[x, y].height[3] += 1;
                            continue;
                        }
                    }

                    if (IsInRange(x + 1, y + 1))
                    {
                        int height = tiles[x + 1, y + 1].GetMaxHeight();
                        if (AllHeightsHigher(tiles[x + 1, y + 1].height, h) && (height == h + 1 || height == h + 2))
                        {
                            tiles[x, y].height[2] += 1;
                            continue;
                        }
                    }

                    if (IsInRange(x + 1, y - 1))
                    {
                        int height = tiles[x + 1, y - 1].GetMaxHeight();
                        if (AllHeightsHigher(tiles[x + 1, y - 1].height, h) && (height == h + 1 || height == h + 2))
                        {
                            tiles[x, y].height[1] += 1;
                            continue;
                        }
                    }


                }
            }


        }

    }   
}
