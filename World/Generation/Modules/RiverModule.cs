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
        Random random;
        Dictionary<(Room, Room), List<Point>> pathCache = new Dictionary<(Room, Room), List<Point>>();
        private const int SMOOTHING_PASSES = 2;

        public RiverModule(List<Room> waters, Random random)
        {
            this.waters = waters;
            this.random = random;
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
            if (!param.hasRivers)
                return;

            List<HashSet<Point>> w = GenHelper.FloodFill(tiles, (t) => t.type == TileType.Water);
            waters = w.ConvertAll((hs) => new Room(hs));

            if (waters.Count < 2)
                return;            

            List<HashSet<Room>> connectableRooms = new List<HashSet<Room>>();

            Stopwatch sw1 = new Stopwatch();
            sw1.Start();

            List<HashSet<Point>> groundFills = GenHelper.FloodFill(tiles, (t) => t.GetMaxHeight() == param.minHeight);

            foreach(HashSet<Point> points in groundFills)
            {
                HashSet<Room> rooms = new HashSet<Room>();
                foreach(Room r in waters)
                {
                    if(r.Tiles.Any((p) => points.Contains(p)))                    
                        rooms.Add(r);                    
                }
                connectableRooms.Add(rooms);
            }

            sw1.Stop();
            Debug.WriteLine($"Length of finding to connecting rooms: {sw1.ElapsedMilliseconds}");

            Stopwatch sw2 = new Stopwatch();
            sw2.Start();

            foreach (HashSet<Room> roomSet in connectableRooms)
            {
                RoomGraph graph = new RoomGraph();                
                graph.AddRoomsAndConnectAll(roomSet.ToList());
                var minSpanTree = graph.MinSpanningTree(random, (r1, r2) => {
                    List<Point> path = null;
                    if (pathCache.ContainsKey((r1, r2)))
                        path = pathCache[(r1, r2)];
                    else if (pathCache.ContainsKey((r2, r1)))
                        path = pathCache[(r2, r1)];
                    else
                    {
                        path = GenHelper.AStar(tiles, r1.MiddlePoint, r2.MiddlePoint, (t) => t.GetMaxHeight() == param.minHeight && t.AllHeightsAreSame(), (t) => t.type == TileType.Water ? 1f : 5f, GenHelper.IterateNeighboursFourDir, false);
                        if (path == null)                        
                            return int.MaxValue;
                        else
                            pathCache.Add((r1, r2), path);
                    }
                    
                    return path.Count;
                }).ToConnectionList();

                foreach((Room r1, Room r2) in minSpanTree){
                    ConnectWaters(param, tiles, r1, r2);                    
                }
            }
            sw2.Stop();
            Debug.WriteLine($"Connecting all the rooms: {sw2.ElapsedMilliseconds}");

        }

        private void SmoothWaterTiles(GeneratorParameter param, Tile[,] tiles)
        {
            for (int i = 0; i < SMOOTHING_PASSES; i++)
            {
                for (int x = 0; x < param.size.X; x++)
                {
                    for (int y = 0; y < param.size.Y; y++)
                    {
                        if (tiles[x, y].AllHeightsAreSame())
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
                            } else if(waterNeighbours <= 2)
                            {
                                tiles[x, y].type = TileType.Nothing;
                            }

                        }
                    }
                }
            }            
        }

        const int DIST_PER_POINT = 1;
        const float MAX_DIST = 25;

        private void ConnectWaters(GeneratorParameter param, Tile[,]tiles, Room a, Room b)
        {            
            Point source = a.MiddlePoint;
            Point target = b.MiddlePoint;            
            int height = param.minHeight;
            List<Point> directPath = null;

            if (pathCache.ContainsKey((a,b)))
                directPath = pathCache[(a,b)];
            else if (pathCache.ContainsKey((b, a)))
                directPath = pathCache[(b, a)];

            int from = 0; //find the index of the last tile in the path, that is inside Room a
            while(from < directPath.Count - 1 && a.Tiles.Contains(directPath[from + 1]))
            {
                from += 1;
            }
            int to = directPath.Count - 1; //find the index of the last tile in the path, that is inside Room b
            while (to > 0 && b.Tiles.Contains(directPath[to - 1]) )
            {
                to -= 1;
            }

            List<Point> points = new List<Point>(directPath.Count / DIST_PER_POINT + 1);

            for (int i = from; i <= to; i += DIST_PER_POINT)
            {
                Point p = directPath[i];                                                                
                if(tiles[p.X, p.Y].type == TileType.Water)
                {
                    points.Add(p);                    
                    continue;
                }

                Vector2 direction = (target.ToVector2() - p.ToVector2());
                direction.Normalize();
                Vector2 dirLeft = new Vector2(-direction.Y, direction.X);
                Vector2 dirRight = new Vector2(direction.Y, -direction.X);                

                //Now find the middle between the hills left and right.                
                bool foundRight = false;
                float distRight = 0f;
                bool foundLeft = false;
                float distLeft = 0f;

                int count = 1;

                Func<Point, Vector2, int, Point> GetPoint = (Point point, Vector2 dir, int c) => {
                    return point + new Point((int)Math.Round(dir.X * c), (int)Math.Round(dir.Y * c));
                };

                while (!foundRight || !foundLeft)
                {
                    if (!foundRight)
                    {
                        Point r = GetPoint(p, dirRight, count);
                        if (!GenHelper.IsInRange(r) || tiles[r.X, r.Y].GetMaxHeight() > height)
                        {
                            foundRight = true;
                            distRight = (p.ToVector2() - r.ToVector2()).Length();
                        }
                    }

                    if (!foundLeft)
                    {
                        Point l = GetPoint(p, dirLeft, count);
                        if (!GenHelper.IsInRange(l) || tiles[l.X, l.Y].GetMaxHeight() > height)
                        {
                            foundLeft = true;
                            distLeft = (p.ToVector2() - l.ToVector2()).Length();
                        }
                    }

                    count++;
                }

                //adjust if the movement left or right would be too big
                if(distLeft > MAX_DIST)
                {
                    float ratio = MAX_DIST / distLeft;
                    distLeft = MAX_DIST;
                    distRight *= ratio;
                }
                if(distRight > MAX_DIST)
                {
                    float ratio = MAX_DIST / distRight;
                    distRight = MAX_DIST;
                    distLeft *= ratio;
                }

                float distTotalHalf = (distRight + distLeft) / 2f;
                Point newPoint;
                float stretch = 1.025f - (float)random.NextDouble() / 5.0f; //used to stretch the distance +-2.5%

                if(distLeft > distRight)
                {
                    float dist = (distLeft - distTotalHalf) * stretch;
                    newPoint = p + (dirLeft * dist).ToPoint();
                } else
                {
                    float dist = (distRight - distTotalHalf) *stretch;
                    newPoint = p + (dirRight * dist).ToPoint();
                }

                p = newPoint;
                points.Add(p);
            }
            if(from - 1 >= 0)
                points[0] = directPath[from - 1];

            if (!b.Tiles.Contains(points.Last()))
            {
                if(to + 1 < directPath.Count)
                    points.Add(directPath[to + 1]);
                else
                    points.Add(directPath[to]);
            }
                                
            //width of river only in steps of two, because the width would look different for straight or angled parts of the river
            int width = random.Next(1, 5) * 2; 
                                    
            for (int i = 0; i < points.Count - 1; i++)
            {
                Point p1 = points[i];
                Point p2 = points[i + 1];
                //tiles[p1.X, p1.Y].color = Color.HotPink;
                //tiles[p2.X, p2.Y].color = Color.HotPink;
                List<Point> path = GenHelper.AStar(tiles, p1, p2, (t) => t.GetMaxHeight() == param.minHeight && t.AllHeightsAreSame(), (t) => 1f, GenHelper.IterateNeighboursFourDir, false); 

                if (path != null)
                {
                    foreach (Point p in path)
                    {
                        tiles[p.X, p.Y].type = TileType.Water;

                        for (int j = 0; j < width; j++)
                        {                          
                            Point offset;
                            switch (j)
                            {
                                case 0:
                                    offset = new Point(1, 0);
                                    break;
                                case 1:
                                    offset = new Point(0, -1);
                                    break;
                                case 2:
                                    offset = new Point(-1, 0);
                                    break;
                                case 3:
                                default:
                                    offset = new Point(0, 1);
                                    break;
                                case 4:
                                    offset = new Point(2, 1);
                                    break;
                                case 5:
                                    offset = new Point(1, -2);
                                    break;
                                case 6:
                                    offset = new Point(-2, 1);
                                    break;
                                case 7:
                                    offset = new Point(-1, 2);
                                    break;

                            }

                            Point point = p + offset;
                            if (GenHelper.IsInRange(point) && tiles[point.X, point.Y].GetMaxHeight() == param.minHeight)
                            {
                                tiles[point.X, point.Y].type = TileType.Water;
                            }
                        }
                    }                    
                }
            }           

        }
    }
}
