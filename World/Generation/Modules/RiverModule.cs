using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.World.Generation.Modules
{
    public class RiverModule : IGeneratorModule
    {
        List<Room> waters;

        public RiverModule(List<Room> waters)
        {
            this.waters = waters;
        }

        public void Apply(GeneratorParameter param, Tile[,] tiles)
        {
            if (param.hasWater)
            {
                CreateRivers(param, tiles);

                SmoothWaterTiles(param, tiles);

            }
        }

        private void CreateRivers(GeneratorParameter param, Tile[,] tiles)
        {
            List<HashSet<Point>> w = GenHelper.FloodFill(tiles, (t) => t.type == TileType.Water);
            waters = w.ConvertAll((hs) => new Room(hs));

            if (waters.Count < 2)
                return;

            //TODO: Only connect min graph lakes
            for (int i = 0; i < waters.Count - 1; i++)
            {
                ConnectWaters(param, tiles, waters[i], waters[i + 1]);
                /*for (int k = i+1; k < waters.Count; k++)
                {
                    ConnectWaters(waters[i], waters[k]);   
                }*/
            }
        }

        private void SmoothWaterTiles(GeneratorParameter param, Tile[,] tiles)
        {
            int smoothed = 0;
            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    if (tiles[x, y].type != TileType.Water && tiles[x, y].AllHeightsAreSame())
                    {
                        int waterNeighbours = 0;
                        foreach (Point n in GenHelper.IterateNeighboursEightDir(x, y))
                        {
                            if (tiles[n.X, n.Y].type == TileType.Water)
                            {
                                waterNeighbours++;
                            }
                        }
                        if (waterNeighbours >= 5)
                        {
                            tiles[x, y].type = TileType.Water;
                            smoothed++;
                        }

                    }
                }
            }
            //Debug.WriteLine($"Water Smooted {smoothed}");
        }

        private void ConnectWaters(GeneratorParameter param, Tile[,]tiles, Room a, Room b)
        {
            Point sourceMid = a.MiddlePoint;
            Point targetMid = b.MiddlePoint;

            Point source = sourceMid;

            Point target = targetMid;


            Vector2 direction = (target.ToVector2() - source.ToVector2());
            direction.Normalize();

            Vector2 dirLeft = new Vector2(-direction.Y, direction.X);
            Vector2 dirRight = new Vector2(direction.Y, -direction.X);

            Func<Tile, float> cost = (t) => {

                int h = param.minHeight;
                Point coord = t.coord;
                bool foundRight = false;
                float distRight = 0f;
                bool foundLeft = false;
                float distLeft = 0f;

                int count = 1;

                while (!foundRight || !foundLeft)
                {
                    if (!foundRight)
                    {
                        Point r = coord + new Point((int)Math.Round(dirRight.X * count), (int)Math.Round(dirRight.Y * count));
                        if (!GenHelper.IsInRange(r) || tiles[r.X, r.Y].GetMaxHeight() > h)
                        {
                            foundRight = true;
                            distRight = (coord.ToVector2() - r.ToVector2()).Length();
                        }
                    }

                    if (!foundLeft)
                    {
                        Point l = coord + new Point((int)Math.Round(dirLeft.X * count), (int)Math.Round(dirLeft.Y * count));
                        if (!GenHelper.IsInRange(l) || tiles[l.X, l.Y].GetMaxHeight() > h)
                        {
                            foundLeft = true;
                            distLeft = (coord.ToVector2() - l.ToVector2()).Length();
                        }
                    }

                    count++;
                }

                float min = Math.Min(distRight, distLeft);
                float max = Math.Max(distRight, distLeft);

                float proportion = min / max;

                float c = 2f - proportion * 2f;

                Debug.WriteLine($"{min} / {max} = {proportion} => {c}");

                return c;
            };

            List<Point> path = GenHelper.AStar(tiles, source, target, (t) => t.GetMaxHeight() == param.minHeight && t.AllHeightsAreSame(), cost, false);

            if (path != null)
            {
                foreach (Point p in path)
                {
                    tiles[p.X, p.Y].type = TileType.Water;

                    Point p2 = p + new Point(1, 1);
                    if (GenHelper.IsInRange(p2) && tiles[p2.X, p2.Y].GetMaxHeight() == param.minHeight)
                        tiles[p2.X, p2.Y].type = TileType.Water;
                }

            }
        }
    }
}
