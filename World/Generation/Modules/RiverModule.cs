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

            //check every lake against every other lake and if they are connectable (a path between them where every tile is the same height as the lakes) 
            // put them into a hashset together. If there already is a list with one of the lakes put the other lake into that list, 
            // thereby finding sets of connectable lakes.
            for(int i = 0; i < waters.Count; ++i)
            {
                Room a = waters[i];
                for (int j = i + 1; j < waters.Count; ++j)
                {
                    Room b = waters[j];
                    List<Point> path = GenHelper.AStar(tiles, a.MiddlePoint, b.MiddlePoint, (t) => t.GetMaxHeight() == param.minHeight && t.AllHeightsAreSame(), (t) => 1f, false);
                    
                    if(path != null) //means a and b are connectible
                    {
                        bool inserted = false;
                        foreach(HashSet<Room> roomSet in connectableRooms)
                        {
                            if (roomSet.Contains(a))      
                                roomSet.Add(b);
                            else if (roomSet.Contains(b))                            
                                roomSet.Add(a);
                            else                            
                                continue;
                            
                            inserted = true;
                            break;
                        }
                        if (!inserted)
                        {
                            HashSet<Room> newSet = new HashSet<Room>();
                            newSet.Add(a);
                            newSet.Add(b);
                            connectableRooms.Add(newSet);
                        }
                    }

                }
            }

            foreach (HashSet<Room> roomSet in connectableRooms)
            {
                RoomGraph graph = new RoomGraph();
                graph.AddRoomsAndConnectAll(waters);
                var minSpanTree = graph.MinSpanningTree(random, (r1, r2) => {
                List<Point> path = GenHelper.AStar(tiles, r1.MiddlePoint, r2.MiddlePoint, (t) => t.GetMaxHeight() == param.minHeight && t.AllHeightsAreSame(), (t) => 1f, false);
                    double dist = 0.0;
                    if (path == null)
                        return double.MaxValue;
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        dist += (path[i].ToVector2() - path[i + 1].ToVector2()).Length();
                    }
                    return dist;
                }).ToConnectionList();
                
                foreach((Room r1, Room r2) in minSpanTree){
                    ConnectWaters(param, tiles, r1, r2);
                }

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
        }

        const float DIST_PER_POINT = 3f;
        const float MAX_DIST = 20;

        private void ConnectWaters(GeneratorParameter param, Tile[,]tiles, Room a, Room b)
        {            
            Point source = a.MiddlePoint;
            Point target = b.MiddlePoint;

            Vector2 direction = (target.ToVector2() - source.ToVector2());
            float distance = direction.Length();
            direction.Normalize();            

            int numPoints = (int)(distance / DIST_PER_POINT + 0.5);
            int height = param.minHeight;
            List<Point> points = new List<Point>(numPoints);

            while(!b.Tiles.Contains(source))
            {                
                points.Add(source);
                source = source + (direction * DIST_PER_POINT).ToPoint();

                if(GenHelper.IsInRange(source) && tiles[source.X, source.Y].type == TileType.Water)
                {                    
                    continue;
                }

                Vector2 dirLeft = new Vector2(-direction.Y, direction.X);
                Vector2 dirRight = new Vector2(direction.Y, -direction.X);                

                //Now find the middle between the hills left and right.                
                bool foundRight = false;
                float distRight = 0f;
                bool foundLeft = false;
                float distLeft = 0f;

                int count = 1;

                Func<Point, Vector2, int, Point> GetPoint = (Point p, Vector2 dir, int c) => {
                    return p + new Point((int)Math.Round(dir.X * c), (int)Math.Round(dir.Y * c));
                };

                while (!foundRight || !foundLeft)
                {
                    if (!foundRight)
                    {
                        Point r = GetPoint(source, dirRight, count); //coord + new Point((int)Math.Round(dirRight.X * count), (int)Math.Round(dirRight.Y * count));
                        if (!GenHelper.IsInRange(r) || tiles[r.X, r.Y].GetMaxHeight() > height) //|| tiles[r.X, r.Y].type == TileType.Water)
                        {
                            foundRight = true;
                            distRight = (source.ToVector2() - r.ToVector2()).Length();
                        }
                    }

                    if (!foundLeft)
                    {
                        Point l = GetPoint(source, dirLeft, count); //coord + new Point((int)Math.Round(dirLeft.X * count), (int)Math.Round(dirLeft.Y * count));
                        if (!GenHelper.IsInRange(l) || tiles[l.X, l.Y].GetMaxHeight() > height) //|| tiles[l.X, l.Y].type == TileType.Water)
                        {
                            foundLeft = true;
                            distLeft = (source.ToVector2() - l.ToVector2()).Length();
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
                    newPoint = source + (dirLeft * dist).ToPoint();
                } else
                {
                    float dist = (distRight - distTotalHalf) *stretch;
                    newPoint = source + (dirRight * dist).ToPoint();
                }

                source = newPoint;

                direction = (target.ToVector2() - source.ToVector2());
                direction.Normalize();
            }

            points.Add(source);

            //width of river only in steps of two, because the width would look different for straight or angled parts of the river
            int width = random.Next(1, 5) * 2; 
                                    
            for (int i = 0; i < points.Count - 1; i++)
            {
                Point p1 = points[i];
                Point p2 = points[i + 1];
                //tiles[p1.X, p1.Y].color = Color.HotPink;
                //tiles[p2.X, p2.Y].color = Color.HotPink;
                List<Point> path = GenHelper.AStar(tiles, p1, p2, (t) => t.GetMaxHeight() == param.minHeight && t.AllHeightsAreSame(), (t) => 1f, false); 

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
