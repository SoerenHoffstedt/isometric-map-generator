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
        private Random random;

        public CityConnectionModule(List<Room> cities, Random random)
        {
            this.cities = cities;
            this.random = random;
        }        

        public void Apply(GeneratorParameter param, Tile[,] tiles)
        {
            if (!param.hasCityConnections)
                return;

            List<(Room, Room)> toConnect = GetRoomsToConnect();

            foreach ((Room r, Room r2) in toConnect)
            {
                Debug.Assert(r != r2);
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

                if (r2 != null && r2.Tiles.Count > 5)
                {


                    Point target = r2.MiddlePoint; //TODO: get road tile of nearest[l].Tiles nearest to start
                    float targetDist = float.MaxValue;
                    foreach (Point p in r2.Tiles)
                    {
                        if (tiles[p.X, p.Y].type == TileType.Road && tiles[p.X, p.Y].AllHeightsAreSame())
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

        private List<(Room, Room)> GetRoomsToConnect()
        {
            RoomGraph graph = new RoomGraph();
            graph.AddNodesAndConnectAll(cities);
            RoomGraph minSpan = graph.MinSpanningTree(random);                     
            List<(Room, Room)> toConnect = minSpan.ToConnectionList();

            return toConnect;
        }
        
    }
}
