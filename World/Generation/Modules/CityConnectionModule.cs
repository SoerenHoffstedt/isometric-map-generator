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
                    Point target = r2.MiddlePoint;
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

                    Func<Tile, bool> IsWalkable = (t) => t.IsRoadPlaceable();
                    Func<Tile, float> Cost = (t) =>
                    {
                        if (t.type == TileType.Road)
                            return 1f;
                        else if (t.type == TileType.Water)
                            return 7f;
                        else if (t.type == TileType.Forest)
                            return 3f;
                        else if (t.type == TileType.House)
                            return 20f;
                        else
                            return 3f;
                    };
                    
                    List<Point> path = GenHelper.AStar(tiles, start, target, IsWalkable, Cost, GenHelper.IterateNeighboursFourDirRoads);
                    
                    if (path != null && !(path.Count == 1 && path[0] == new Point(-1, -1)))
                    {
                        for (int k = 0; k < path.Count; k++)
                        {
                            Point p = path[k];                            
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
            graph.AddRoomsAndConnectAll(cities);
            RoomGraph minSpan = graph.MinSpanningTree(random, (r1, r2) => r1.DistanceToSquared(r2));

            foreach(Room r in cities)
            {
                foreach(Room other in cities)
                {
                    if (r == other || minSpan.connections[r].Contains(other) )
                        continue;
                    
                    double directDist = r.DistanceTo(other);
                    double distInGraph = GetDistanceInGraph(minSpan, r, other);

                    if(directDist * 1.5 < distInGraph)
                    {
                        minSpan.AddConnectionBothWays(r, other);                        
                    }
                }
            }

            return minSpan.ToConnectionList();
        }

        private double GetDistanceInGraph(RoomGraph graph, Room from, Room to)
        {
            //use Astar. My generic astar implementation is only suitable for 2D arrays at the moment, so reimplement it here...
            Dictionary<Room, Room> prev = new Dictionary<Room, Room>(graph.Count);
            Dictionary<Room, double> cost = new Dictionary<Room, double>(graph.Count);
            bool targetReached = false;

            foreach(Room r in graph.rooms)
            {
                prev.Add(r, null);
                cost.Add(r, double.MaxValue);
            }

            var closedList = new HashSet<Room>();
            var openList = new Barely.Util.Priority_Queue.SimplePriorityQueue<Room>();
            openList.Enqueue(from, 0);
            cost[from] = 0;
            prev[from] = null;

            while(openList.Count > 0)
            {
                Room currentRoom = openList.Dequeue();

                if (currentRoom == to)
                {
                    targetReached = true;
                    break;
                }
             
                closedList.Add(currentRoom);
                foreach (Room n in graph.connections[currentRoom])
                {
                    if (closedList.Contains(n))
                        continue;

                    double tenativeCost = cost[currentRoom] + currentRoom.DistanceTo(n);

                    bool contains = openList.Contains(n);
                    if (contains && tenativeCost >= cost[n])
                        continue;

                    prev[n] = currentRoom;
                    cost[n] = tenativeCost;

                    tenativeCost += n.DistanceTo(to);

                    if (contains)
                        openList.UpdatePriority(n, tenativeCost);
                    else
                        openList.Enqueue(n, tenativeCost);

                }
                
            }

            Debug.Assert(cost[to] < double.MaxValue);
            return cost[to];
        }

    }
}
