using Barely.ProgGen;
using Barely.Util.Priority_Queue;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.World.Generation
{
    public static class GenHelper
    {
        public static Point Size;

        public static IEnumerable<Point> IterateNeighboursFourDir(int x, int y)
        {
            if (IsInRange(x - 1, y))
                yield return new Point(x - 1, y);
            if (IsInRange(x + 1, y))
                yield return new Point(x + 1, y);
            if (IsInRange(x, y - 1))
                yield return new Point(x, y - 1);
            if (IsInRange(x, y + 1))
                yield return new Point(x, y + 1);
        }

        public static IEnumerable<Point> IterateNeighboursEightDir(int x, int y)
        {
            for (int xx = x - 1; xx <= x + 1; xx++)
            {
                for (int yy = y - 1; yy <= y + 1; yy++)
                {
                    if (IsInRange(xx, yy) && (xx != x || yy != y))
                    {
                        yield return new Point(xx, yy);
                    }
                }
            }
        }

        public static IEnumerable<Point> IterateCornerNeighbours(int x, int y)
        {
            if (IsInRange(x - 1, y - 1))
                yield return new Point(x - 1, y - 1);

            if (IsInRange(x - 1, y + 1))
                yield return new Point(x - 1, y + 1);

            if (IsInRange(x + 1, y + 1))
                yield return new Point(x + 1, y + 1);

            if (IsInRange(x + 1, y - 1))
                yield return new Point(x + 1, y - 1);

        }

        public static bool IsInRange(Point p)
        {
            return IsInRange(p.X, p.Y);
        }

        public static  bool IsInRange(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Size.X && y < Size.Y;
        }

        public static List<HashSet<Point>> FloodFill<T>(T[,] map, Func<T, bool> Pred, int listCapacity = 32)
        {
            List<HashSet<Point>> toReturn = new List<HashSet<Point>>(listCapacity);
            HashSet<Point> alreadyVisited = new HashSet<Point>();

            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    Point pp = new Point(x, y);
                    if (Pred(map[x, y]) && !alreadyVisited.Contains(pp))
                    {
                        HashSet<Point> set = new HashSet<Point>();
                        Queue<Point> q = new Queue<Point>(256);

                        q.Enqueue(pp);
                        alreadyVisited.Add(pp);

                        while (q.Count > 0)
                        {
                            Point p = q.Dequeue();
                            set.Add(p);

                            foreach (Point n in IterateNeighboursFourDir(p.X, p.Y))
                            {
                                if (Pred(map[n.X, n.Y]) && !alreadyVisited.Contains(n))
                                {
                                    q.Enqueue(n);
                                    alreadyVisited.Add(n);
                                }
                            }
                        }
                        toReturn.Add(set);
                    }
                }
            }

            return toReturn;
        }

        public static List<Room> GetCellularAutomataAsRooms(int smoothIterations, int blockingTilePercentage, bool openCave)
        {
            bool[,] automata = CellularAutomata.Generate(Size, smoothIterations, blockingTilePercentage, openCave);

            List<HashSet<Point>> bla = FloodFill(automata, (b) => { return !b; });

            return bla.ConvertAll((set) => { return new Room(set); });
        }

        public static List<Point> AStar<T>(T[,] map, Point source, Point target, Func<T, bool> IsWalkable, Func<T, float> WalkCost, bool reversePath = true) where T : class
        {
            Dictionary<Point, Point> prev = new Dictionary<Point, Point>();
            Dictionary<Point, float> cost = new Dictionary<Point, float>();
            
            for (int x = 0; x < map.GetLength(0); x++)
            {
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    Point n = new Point(x, y);
                    if (n != source)
                    {
                        cost.Add(n, float.MaxValue);
                        prev.Add(n, new Point(-1, -1));
                    }
                }
            }

            bool targetReached = false;

            var closedList = new HashSet<Point>();
            var openList = new SimplePriorityQueue<Point>();
            openList.Enqueue(source, 0);
            cost[source] = 0;
            prev[source] = new Point(-1, -1);

            while (openList.Count > 0)
            {
                Point currentNode = openList.Dequeue();

                if (currentNode == target)
                {
                    targetReached = true;
                    break;
                }

                if (IsWalkable(map[currentNode.X, currentNode.Y]))
                {
                    closedList.Add(currentNode);
                    foreach (Point n in IterateNeighboursFourDir(currentNode.X, currentNode.Y))
                    {
                        if (closedList.Contains(n))
                            continue;

                        float tenativeCost = cost[currentNode] + WalkCost(map[currentNode.X, currentNode.Y]);

                        bool contains = openList.Contains(n);
                        if (contains && tenativeCost >= cost[n])
                            continue;

                        prev[n] = currentNode;
                        cost[n] = tenativeCost;

                        tenativeCost += (target - n).ToVector2().Length();

                        if (contains)
                            openList.UpdatePriority(n, tenativeCost);
                        else
                            openList.Enqueue(n, tenativeCost);

                    }
                }

            }

            if (!targetReached)
                return null;

            List<Point> path = new List<Point>();
            Point run = target;
            path.Add(run);
            while (run != source)
            {
                run = prev[run];
                path.Add(run);
            }

            path.Reverse();
            return path;
        }

    }
}
