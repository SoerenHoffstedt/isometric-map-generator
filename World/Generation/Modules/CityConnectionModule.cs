using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.World.Generation.Modules
{
    public class CityConnectionModule : IGeneratorModule
    {
        private List<Room> cities;

        public CityConnectionModule(List<Room> cities)
        {
            this.cities = cities;
        }

        public void Apply(GeneratorParameter param, Tile[,] tiles)
        {
            HashSet<Connection> connections = new HashSet<Connection>();

            for (int i = 0; i < cities.Count; i++)
            {
                Room r = cities[i];

                if (r == null || r.Tiles.Count < 5)
                    continue;

                Point start = r.MiddlePoint;

                //get a road tile near the middle point, if it is not a road
                if (tiles[start.X, start.Y].type != TileType.Road)
                {
                    for (int x = start.X - 2; x <= start.X + 2; x++)
                    {
                        for (int y = start.Y - 2; y <= start.Y + 2; y++)
                        {
                            if (GenHelper.IsInRange(x, y) && tiles[x, y].type == TileType.Road)
                            {
                                start = new Point(x, y);
                                break;
                            }
                        }
                        if (tiles[start.X, start.Y].type == TileType.Road)
                            break;
                    }
                }

                Debug.Assert(tiles[start.X, start.Y].type == TileType.Road);

                //get the three nearest cities. This is dumb. Replace it with somethin sane.
                Room[] nearest = new Room[3];
                for (int k = 0; k < cities.Count; k++)
                {
                    if (k == i)
                        continue;

                    Room r2 = cities[k];

                    float distance = (r.MiddlePoint - r2.MiddlePoint).ToVector2().LengthSquared();

                    bool placed = false;

                    for (int l = 0; l < 3; l++)
                    {
                        if (nearest[l] == null)
                        {
                            nearest[l] = r2;
                            placed = true;
                            break;
                        }
                    }
                    if (!placed)
                    {
                        for (int l = 0; l < 3; l++)
                        {
                            float oldDistance = (r.MiddlePoint - nearest[l].MiddlePoint).ToVector2().LengthSquared();
                            if (distance < oldDistance)
                            {
                                nearest[l] = r2;
                                break;
                            }
                        }
                    }
                }

                for (int l = 0; l < 3; l++)
                {
                    if (nearest[l] != null && nearest[l].Tiles.Count > 5)
                    {


                        Point target = nearest[l].MiddlePoint; //TODO: get road tile of nearest[l].Tiles nearest to start
                        float targetDist = float.MaxValue;
                        foreach (Point p in nearest[l].Tiles)
                        {
                            if (tiles[p.X, p.Y].type == TileType.Road)
                            {
                                float dist = (start - p).ToVector2().LengthSquared();
                                if (dist < targetDist)
                                {
                                    target = p;
                                    targetDist = dist;
                                }
                            }
                        }
                        Debug.Assert(tiles[target.X, target.Y].type == TileType.Road);


                        Func<Tile, bool> IsWalkable = (t) => t.IsRoadPlaceable() && t.type != TileType.House;
                        Func<Tile, float> Cost = (t) =>
                        {
                            if (t.type == TileType.Road)
                                return 1f;
                            else if (t.type == TileType.Water)
                                return 7f;
                            else if (t.type == TileType.Forest)
                                return 4f;
                            else
                                return 4f;
                        };

                        List<Point> path = GenHelper.AStar(tiles, start, target, IsWalkable, Cost);
                        if (path != null && !(path.Count == 1 && path[0] == new Point(-1, -1)))
                        {
                            for (int k = 0; k < path.Count; k++)
                            {
                                Point p = path[k];
                                //if(k == 0 || k == path.Count - 1)
                                //     tiles[p.X, p.Y].color = Color.Green;

                                tiles[p.X, p.Y].type = TileType.Road;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Target not reached");
                        }

                    }
                }

            }
        }
    }
}
