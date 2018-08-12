using Industry.Simulation;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Industry.World.Generation.GenHelper;

namespace Industry.World.Generation.Modules
{
    public class CityModule : IGeneratorModule
    {
        private Point chunkPerCity = new Point(32, 32);
        private const float minEdgeDistance = 49.0f;
        private const int CITY_MIN_SIZE = 15;

        private GeneratorParameter param;
        private Tile[,] tiles;
        private List<Room> cities;
        private Random random;

        const double levelPlusMinus = 0.1;
        const double levelThreePer = 0.5;
        const double levelOnePer = 0.2;

        public CityModule(List<Room> cities, Random random)
        {
            this.cities = cities;
            this.random = random;
        }

        public void Apply(GeneratorParameter param, Tile[,] tiles)
        {
            if (!param.hasCities)
                return;

            this.param = param;
            this.tiles = tiles;

            int numCities = GetNumberOfCities();
            int notPlaceableCities = 0;
            List<Point> cityCenters = new List<Point>(numCities);

            for (int i = 0; i < numCities; i++)
            {
                Point center = NewCityCenter(cityCenters);
                if (!IsInRange(center))
                {
                    notPlaceableCities++;
                    continue;
                }

                cityCenters.Add(center);

                Room room = GrowCity(center, GetCitySize());
                cities.Add(room);
            }

            Debug.WriteLine($"Number of cities: {numCities}. Not placeable were: {notPlaceableCities}");

        }

        private Room GrowCity(Point start, int size)
        {
            Room room = new Room();
            Point blockSize = new Point(4, 6);
            int blockCount = 0;
            Point origin = start;

            Queue<Intersection> intersections = new Queue<Intersection>(64);
            //todo: make sure origin can have a road.
            intersections.Enqueue(new Intersection(origin, true, true, true, true));

            const int ROAD_STEPS_MIN = 3;
            const int ROAD_STEPS_MAX = 5;

            Direction[] directions = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };

            double levelThree = GetPercentageOfCitizenLevel(CitizenLevel.Three);
            double levelOne = GetPercentageOfCitizenLevel(CitizenLevel.One);

            bool leftRightIsShort = random.NextDouble() > 0.5 ? true : false;


            /*
             * Direction <-> axis
             * 
                up:     -y
                down:    y
                left:   -x
                right:   x
             
             */

            while (intersections.Count > 0 && blockCount < size)
            {
                Intersection curr = intersections.Dequeue();
                Point pos = curr.pos;
                if (!IsInRange(pos))
                    continue;
                tiles[pos.X, pos.Y].type = TileType.Road;

                foreach (Direction d in directions)
                {
                    if (!curr.HasDirection(d))
                        continue;

                    int steps = ROAD_STEPS_MIN;

                    if (leftRightIsShort && (d == Direction.Left || d == Direction.Right))
                    {
                        steps = ROAD_STEPS_MIN;
                    }
                    else if (!leftRightIsShort && (d == Direction.Right || d == Direction.Down))
                    {
                        steps = ROAD_STEPS_MIN;
                    }
                    else
                    {
                        steps = random.Next(ROAD_STEPS_MIN, ROAD_STEPS_MAX + 1);
                    }

                    Point p = pos;

                    //check if the same direction already has roads on neighbouring tiles.
                    switch (d)
                    {
                        case Direction.Up:
                            p.Y -= 1;
                            break;
                        case Direction.Right:
                            p.X += 1;
                            break;
                        case Direction.Down:
                            p.Y += 1;
                            break;
                        case Direction.Left:
                            p.X -= 1;
                            break;
                    }
                    if (DirectionNeighbourIsRoad(p, d))
                        continue;

                    p = pos;

                    for (int i = 0; i < steps; i++)
                    {
                        switch (d)
                        {
                            case Direction.Up:
                                p.Y -= 1;
                                break;
                            case Direction.Right:
                                p.X += 1;
                                break;
                            case Direction.Down:
                                p.Y += 1;
                                break;
                            case Direction.Left:
                                p.X -= 1;
                                break;
                        }

                        if (!IsInRange(p) || !tiles[p.X, p.Y].IsRoadPlaceable(false))
                        {
                            p = new Point(-1, -1);
                            break;
                        }

                        foreach (Point n in DirectionalNeighbours(p, d))
                        {
                            if (IsInRange(n) && GetTile(n).IsHousePlaceable())
                            {
                                Tile t = GetTile(n);

                                double prob = random.NextDouble();
                                CitizenLevel lvl = CitizenLevel.None;
                                if (prob <= levelOne)
                                    lvl = CitizenLevel.One;
                                else if (prob <= levelOne + levelThree)
                                    lvl = CitizenLevel.Three;
                                else
                                    lvl = CitizenLevel.Two;

                                t.type = TileType.House;
                                t.SetCitizenLevel(lvl);
                                t.onTopIndex = param.tileset.GetRandomHouseIndex(t.citizenLevel);

                                blockCount++;
                                room.Add(n);
                            }
                        }

                        room.Add(p);
                        tiles[p.X, p.Y].type = TileType.Road;
                        blockCount++;

                    }

                    if (!IsInRange(p))
                        continue;

                    double dirProb = 0.75;
                    double[] newDirsProbabilities = { dirProb, dirProb, dirProb, dirProb };
                    //dont move back from where we came.
                    switch (d)
                    {
                        case Direction.Up:
                            newDirsProbabilities[2] = 0.0;
                            break;
                        case Direction.Right:
                            newDirsProbabilities[3] = 0.0;
                            break;
                        case Direction.Down:
                            newDirsProbabilities[0] = 0.0;
                            break;
                        case Direction.Left:
                            newDirsProbabilities[1] = 0.0;
                            break;
                    }

                    intersections.Enqueue(new Intersection(p, newDirsProbabilities, random));
                    //intersections.Enqueue(new Intersection(p, random.NextDouble() < dirProb, random.NextDouble() < dirProb, random.NextDouble() < dirProb, random.NextDouble() < dirProb));

                }


            }

            return room;
        }

        Tile GetTile(Point p)
        {
            return tiles[p.X, p.Y];
        }



        private int GetCitySize()
        {
            Point cityChunkBase = chunkPerCity / new Point(4, 4);
            float offset = -(float)random.NextDouble() * param.citySizeRandomOffset;// - param.citySizeRandomOffset / 2f;
            float sizeFactor = param.citySize + offset;
            int grow = (int)((cityChunkBase.X * cityChunkBase.Y) * sizeFactor);
            Debug.WriteLine($"Chunk Base: {cityChunkBase}. City Size: {param.citySize}. Grow size: {grow}. Size factor: {sizeFactor}");
            return cityChunkBase.X * cityChunkBase.Y + grow;
        }

        private Point NewCityCenter(List<Point> citiesAlready)
        {
            float minDistance = (float)(chunkPerCity.X * chunkPerCity.X);

            Point center = new Point(-1, -1);
            int count = 0;
            while (true)
            {
                center.X = random.Next(param.size.X);
                center.Y = random.Next(param.size.Y);

                //test if point is good, than return it, else continue
                Tile tile = tiles[center.X, center.Y];
                if (tile.AllHeightsAreSame() && tile.type != TileType.Water)
                {

                    //test if tile is far enough from an edge
                    int xEdge = (center.X < param.size.X / 2) ? 0 : param.size.X - 1;
                    int yEdge = (center.Y < param.size.Y / 2) ? 0 : param.size.Y - 1;
                    float xDist = (xEdge - center.X) * (xEdge - center.X);
                    float yDist = (yEdge - center.Y) * (yEdge - center.Y);
                    float distToEdge = Math.Min(xDist, yDist);

                    if (distToEdge >= minEdgeDistance)
                    {
                        bool distOkay = true;
                        foreach (Point p in citiesAlready)
                        {
                            float dist = (p.ToVector2() - center.ToVector2()).LengthSquared();
                            if (dist < minDistance)
                            {
                                distOkay = false;
                                break;
                            }
                        }
                        if (distOkay)
                            return center;
                    }
                }

                //if counter too high, abort this shit, or lower the min distance!
                count++;

                if (count > 100000)
                    return new Point(-1, -1);
            }

        }

        private int GetNumberOfCities()
        {
            Point perDim = param.size / chunkPerCity;
            return (int)((perDim.X * perDim.Y) * param.citiesNumber + 0.5f);
        }

        private double GetPercentageOfCitizenLevel(CitizenLevel level)
        {
            double pm = random.NextDouble() * levelPlusMinus * 2.0 - levelPlusMinus;
            Debug.Assert(pm <= levelPlusMinus && pm >= -levelPlusMinus);
            if (level == CitizenLevel.One)
                return levelOnePer + pm;
            else if (level == CitizenLevel.Three)
                return levelThreePer + pm;
            else
                return 0.0;
        }

        private IEnumerable<Point> DirectionalNeighbours(Point pos, Direction dir)
        {
            Point p1 = Point.Zero, p2 = Point.Zero;
            switch (dir)
            {
                case Direction.Up:
                case Direction.Down:
                    p1 = new Point(pos.X - 1, pos.Y);
                    p2 = new Point(pos.X + 1, pos.Y);
                    break;
                case Direction.Right:
                case Direction.Left:
                    p1 = new Point(pos.X, pos.Y - 1);
                    p2 = new Point(pos.X, pos.Y + 1);
                    break;
            }
            yield return p1;
            yield return p2;
        }

        private bool DirectionNeighbourIsRoad(Point pos, Direction dir)
        {
            foreach (Point p in DirectionalNeighbours(pos, dir))
            {
                if (IsInRange(p) && tiles[p.X, p.Y].type == TileType.Road)
                    return true;
            }
            return false;
        }

    }

    [Flags]
    enum Direction
    {
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8
    }

    struct Intersection
    {
        public Point pos;
        public Direction directions;

        public Intersection(Point pos, bool up, bool right, bool down, bool left)
        {
            this.pos = pos;
            directions = 0;
            if (up)
                directions += 1;
            if (right)
                directions += 2;
            if (down)
                directions += 4;
            if (left)
                directions += 8;
        }

        public Intersection(Point pos, double[] dirProbs, Random random)
        {
            this.pos = pos;
            directions = 0;
            if (random.NextDouble() < dirProbs[0])
                directions += 1;
            if (random.NextDouble() < dirProbs[1])
                directions += 2;
            if (random.NextDouble() < dirProbs[2])
                directions += 4;
            if (random.NextDouble() < dirProbs[3])
                directions += 8;

        }

        public bool HasDirection(Direction dir)
        {
            return (dir & directions) > 0;
        }

    }
}
