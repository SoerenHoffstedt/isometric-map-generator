﻿using Industry.Simulation;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Industry.World.Generation.GeneratorHelper;

namespace Industry.World.Generation.Modules
{
    public class CityModule : IGeneratorModule
    {
        private Point chunkPerCity = new Point(32, 32);
        private const float minEdgeDistance = 49.0f;
        private const int CITY_MIN_SIZE = 15;
        private const int RECT_PUSH_DIST = 20;
        private const int PUSHED_OUT_OF_BOUNDS_MAX = 5;

        private GeneratorParameter param;
        private Tile[,] tiles;
        private List<Room> cities;
        private Random random;

        const double levelPlusMinus = 0.1;
        const double levelThreePer = 0.5;
        const double levelOnePer = 0.2;

        /// <summary>
        /// Generate cities by finding random positions for the cities on the map that don't overlap and grow the cities from these positions.
        /// </summary>
        /// <param name="cities">Empty list that the cities will be saved into.</param>
        /// <param name="random"></param>
        public CityModule(List<Room> cities, Random random)
        {
            this.cities = cities;
            this.random = random;
        }

        public void Apply(GeneratorParameter param, Tile[,] tiles)
        {
            if (param.citiesNumber == 0f)
                return;

            this.param = param;
            this.tiles = tiles;

            int numCities = GetNumberOfCities(); //TODO: Propably adjust non placeable.
            int notPlaceableCities = 0;
            List<CityPlacementInfo> cityPlacementInfo = GetCityPlacementInfo(numCities, ref notPlaceableCities);

            foreach (CityPlacementInfo info in cityPlacementInfo)
            {                
                Room room = GrowCity(info);
                if(room != null && room.Tiles.Count > 5)
                    cities.Add(room);
            }

            MergeOverlapingCities();

            int totalSize = 0;
            foreach(Room r in cities)
            {
                totalSize += r.Tiles.Count;
            }
            float avgSize = (float)totalSize / cities.Count;

            foreach (Room cityRoom in cities)
            {
                MakeCityDistricts(cityRoom, avgSize);
            }            

            Debug.WriteLine($"Number of cities: {numCities}. Not placeable were: {notPlaceableCities}. Avg. city size: {avgSize}");
        }

        private void MergeOverlapingCities()
        {
            int count = cities.Count;
            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = i + 1; j < cities.Count; j++)
                {
                    if(cities[i].Tiles.Any((p) => cities[j].Tiles.Contains(p))) // && tiles[p.X, p.Y].type == TileType.House))
                    {
                        cities[i].Tiles.UnionWith(cities[j].Tiles);
                        cities.RemoveAt(j);
                        j--;
                        cities[i].CalculateMiddlePoint();
                    }
                }
            }
            Debug.WriteLine($"Cities merged: {count - cities.Count }");
        }

        #region City districts              

        private const int TILES_PER_DISTRICT = 30;
        private const float PROP_SUBURB = 0.40f;
        private const float PROP_CITY = 0.30f;
        private const float PROP_BUSINESS = 0.12f;
        private const float PROP_INDUSTRY = 0.18f;

        private void MakeCityDistricts(Room cityRoom, float avgCitySize)
        {
            HashSet<Point> totalVisitedPoints = new HashSet<Point>();
            int size = cityRoom.Tiles.Count;

            float totalPoints = size / TILES_PER_DISTRICT;

            int suburbPoints = 0;
            int cityPoints = 0;
            int businessPoints = 0;
            int industryPoints = 0;            

            //set number of points in fixed proportion, sizes of districts will vary based on point placement, so this shouldn't be noticable.
            suburbPoints = (int)(totalPoints * PROP_SUBURB);
            cityPoints = (int)(totalPoints * PROP_CITY);
            businessPoints = (int)(totalPoints * PROP_BUSINESS);
            industryPoints = (int)(totalPoints * PROP_INDUSTRY);

            //if city is small, but not very small and has no business/industry, put one point in.
            if (size < avgCitySize && size > avgCitySize / 2 && industryPoints == 0 && businessPoints == 0)
            {
                if (random.NextDouble() < 0.5)
                    businessPoints = 1;
                else
                    industryPoints = 1;                
            }            

            //due to int casting the number of points will not match totalPoints, so add suburb or city.
            while (suburbPoints + cityPoints + businessPoints + industryPoints < (int)totalPoints)
            {
                if (random.NextDouble() < 0.5)
                    suburbPoints += 1;
                else
                    cityPoints += 1;                
            }

            //for very small cities, just set one suburb point.
            if (suburbPoints + cityPoints + businessPoints + industryPoints >= cityRoom.Tiles.Count)
            {
                suburbPoints = 1;
                cityPoints = 0;
                businessPoints = 0;
                industryPoints = 0;
            }


            //create the fill info for all the districts, generate random point.
            // +1 because later a city point is added in middle
            DistrictFillInfo[] fillInfos = new DistrictFillInfo[suburbPoints + cityPoints + businessPoints + industryPoints + 1];

            for(int i = 0; i < suburbPoints + cityPoints + businessPoints + industryPoints; i++)
            {
                DistrictType lvl = DistrictType.None;
                if (i < suburbPoints)
                    lvl = DistrictType.Suburb;
                else if (i < suburbPoints + cityPoints)
                    lvl = DistrictType.City;
                else if (i < suburbPoints + cityPoints + businessPoints)
                    lvl = DistrictType.Business;
                else if (i < suburbPoints + cityPoints + businessPoints + industryPoints)
                    lvl = DistrictType.Industry;
            
                int index = random.Next(cityRoom.Tiles.Count - i);
                Point position = new Point();

                int counter = 0;
                foreach (Point p in cityRoom.Tiles)
                {
                    if (counter >= index && !totalVisitedPoints.Contains(p))
                    {
                        position = p;
                        break;
                    }
                    counter++;
                }
                fillInfos[i] = new DistrictFillInfo(position, lvl);                
            }

            //add aditional city point in the mid point of the room, for possibility of 
            Point mid = cityRoom.MiddlePoint;            
            fillInfos[fillInfos.Length - 1] = new DistrictFillInfo(mid, DistrictType.City);


            //fill the city from the points, save nearest point and distance to it per Tile
            Dictionary<Tile, int> tileFillPairing = new Dictionary<Tile, int>(cityRoom.Tiles.Count);
            Dictionary<Tile, float> distToCenter = new Dictionary<Tile, float>(cityRoom.Tiles.Count);

            //init dictionaries
            foreach(Point p in cityRoom.Tiles)
            {
                Tile t = tiles[p.X, p.Y];
                tileFillPairing.Add(t, 0);
                distToCenter.Add(t, float.MaxValue);
            }

            //fill city from each point and save the point to a tile if it is shorter than the points that filled before.
            for(int i = 0; i < fillInfos.Length; i++)
            {
                Point start = fillInfos[i].startPoint;
                Queue<Point> toVisit = new Queue<Point>(cityRoom.Tiles.Count);
                HashSet<Point> visited = new HashSet<Point>();
                toVisit.Enqueue(start);
                visited.Add(start);

                while (toVisit.Count > 0)
                {
                    Point p = toVisit.Dequeue();
                    Tile t = tiles[p.X, p.Y];
                    float dist = (p - start).ToVector2().LengthSquared();
                    
                    if(dist < distToCenter[t])
                    {
                        distToCenter[t] = dist;
                        tileFillPairing[t] = i;
                    }
                    foreach(Point n in GeneratorHelper.IterateNeighboursFourDir(p.X, p.Y))
                    {                        
                        if (cityRoom.Tiles.Contains(n) && !visited.Contains(n))
                        {
                            toVisit.Enqueue(n);
                            visited.Add(n);
                        }
                    }
                }

                
            }

            //decide based on nearest point, which house is what district type.
            foreach (Point p in cityRoom.Tiles)
            {
                Tile t = tiles[p.X, p.Y];
                if(t.type == TileType.House)
                {
                    int index = tileFillPairing[t];
                    DistrictType lvl = fillInfos[index].citizenLevel;
                    t.SetCitizenLevel(lvl);
                    t.onTopIndex = param.tileset.GetRandomHouseIndex(lvl, random);
                }
            }
        
        }

        #endregion

        #region City Growth

        private Room GrowCity(CityPlacementInfo info)
        {
            Point start = info.GetCityCenter();
            int size = info.size;
            Room room = new Room();
            Point blockSize = new Point(4, 6);
            int blockCount = 0;
            Point origin = start;

            //when start tile is not flat, search around it for a flat one
            if (!GetTile(origin).AllHeightsAreSame() || GetTile(origin).type == TileType.Water)
            {
                for (int x = -2; x <= 2 && origin == start; x++)
                {
                    for (int y = -2; y < 2; y++)
                    {
                        if(IsInRange(x,y) && tiles[x, y].AllHeightsAreSame() && GetTile(origin).type != TileType.Water)
                        {
                            origin = new Point(x, y);
                            break;
                        }
                            
                    }
                }
            }

            if (tiles[origin.X, origin.Y].type == TileType.Water)
                return null;

            Queue<Intersection> intersections = new Queue<Intersection>(64);
            intersections.Enqueue(new Intersection(origin, true, true, true, true));

            const int ROAD_STEPS_MIN = 3;
            const int ROAD_STEPS_MAX = 5;

            Direction[] directions = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };

            double levelThree = GetPercentageOfCitizenLevel(DistrictType.Business);
            double levelOne = GetPercentageOfCitizenLevel(DistrictType.Suburb);

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
                            if (GetTile(n).IsHousePlaceable())
                            {
                                Tile t = GetTile(n);                         
                                t.type = TileType.House;
                                t.SetCitizenLevel(DistrictType.Suburb);
                                t.onTopIndex = param.tileset.GetRandomHouseIndex(DistrictType.Suburb, random);
                                blockCount++;
                                room.Add(n);
                            }
                        }

                        if (i == steps - 1 && !GetTile(p).AllHeightsAreSame())
                        {
                            continue;
                        }
                        room.Add(p);
                        tiles[p.X, p.Y].type = TileType.Road;
                        blockCount++;

                    }

                    if (!IsInRange(p) || !GetTile(p).AllHeightsAreSame())
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
            float offset = 2 * (float)random.NextDouble() * param.citySizeRandomOffset - param.citySizeRandomOffset;
            float sizeFactor = param.citySize + offset;
            if (sizeFactor <= 0)
                return 0;
            
            int grow = (int)((cityChunkBase.X * cityChunkBase.Y) * sizeFactor);
            int size = cityChunkBase.X * cityChunkBase.Y + grow;
            Debug.WriteLine($"Size: {size}. Chunk Base: {cityChunkBase}. City Size: {param.citySize}. Grow size: {grow}. Size factor: {sizeFactor}");
            return size;
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

        private double GetPercentageOfCitizenLevel(DistrictType level)
        {
            double pm = random.NextDouble() * levelPlusMinus * 2.0 - levelPlusMinus;
            Debug.Assert(pm <= levelPlusMinus && pm >= -levelPlusMinus);
            if (level == DistrictType.Suburb)
                return levelOnePer + pm;
            else if (level == DistrictType.Business)
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
            if (IsInRange(p1))
                yield return p1;
            if (IsInRange(p2))
                yield return p2;
        }

        private bool DirectionNeighbourIsRoad(Point pos, Direction dir)
        {
            foreach (Point p in DirectionalNeighbours(pos, dir))
            {
                if (tiles[p.X, p.Y].type == TileType.Road)
                    return true;
            }
            return false;
        }

        #endregion

        #region City placement

        /// <summary>
        /// Creates a list of placement info for cities. The placement info contains the size of the city and a rectangle of its position. 
        /// The middle point of the rectangle is the position where the city generation is started from.
        /// </summary>
        /// <param name="numCities">The number of cities to create placement info for.</param>
        /// <param name="notPlaceableCities">Counting the not placeable cities (pushed out of bounds)</param>
        /// <returns></returns>
        private List<CityPlacementInfo> GetCityPlacementInfo(int numCities, ref int notPlaceableCities)
        {
            List<CityPlacementInfo> cities = new List<CityPlacementInfo>(numCities);

            for (int i = 0; i < numCities; i++)
            {
                int origSize = GetCitySize();
                int size = (int)(Math.Sqrt(origSize) * 1.2);
                if (size >= param.size.X)
                    size = param.size.X - 1;
                if (size >= param.size.Y)
                    size = param.size.Y - 1;

                int x = random.Next(0, param.size.X - size);
                int y = random.Next(0, param.size.Y - size);
                //size is used as num of blocks, so take the sqrt here assuming a quadratic city. take 1.5x for padding between cities:
                Rectangle rect = new Rectangle(x, y, size, size);
                cities.Add(new CityPlacementInfo(rect, origSize));
            }

            //now move the rects appart!
            while (true)
            {
                bool smthMoved = false;
                bool skip = false;

                for (int a = 0; a < cities.Count && !skip; a++)
                {
                    for (int b = a + 1; b < cities.Count && !skip; b++)
                    {
                        Rectangle rectA = cities[a].rect;
                        Rectangle rectB = cities[b].rect;

                        if (rectA.Intersects(rectB))
                        {
                            //in which direction do the rects have to be seperated? take the axis they are seperated more.
                            int xMove = rectA.X - rectB.X;
                            int yMove = rectA.Y - rectB.Y;

                            if (Math.Abs(xMove) >= Math.Abs(yMove))
                            {
                                //if xMove is positive, push rectA in positive direction, and rectB in negative. if xMove is negative do it the other way around
                                int modifier = xMove > 0 ? 1 : -1;
                                rectA.X += RECT_PUSH_DIST * modifier;
                                rectB.X -= RECT_PUSH_DIST * modifier;
                            }
                            else
                            {
                                int modifier = yMove > 0 ? 1 : -1; //see comment above
                                rectA.Y += RECT_PUSH_DIST * modifier;
                                rectB.Y -= RECT_PUSH_DIST * modifier;
                            }
                            smthMoved = true;

                            //check if one of the rects is pushed out of bounds and move it inside bounds again.
                            if (IsOutOfBounds(rectA))
                            {
                                MoveIntoBounds(ref rectA);
                                cities[a].outOfBoundsCount += 1;
                            }

                            if (IsOutOfBounds(rectB))
                            {
                                MoveIntoBounds(ref rectB);
                                cities[b].outOfBoundsCount += 1;
                            }

                            cities[a].rect = rectA;
                            cities[b].rect = rectB;

                            //if a city is pushed out of bounds again and again, it could end in endless loop, so after X of pushed, remove a city
                            if (cities[a].outOfBoundsCount > PUSHED_OUT_OF_BOUNDS_MAX)
                            {
                                cities.RemoveAt(a);
                                notPlaceableCities += 1;
                                skip = true;
                                b -= 1; //a is smaller than b, so decrease b by one.
                            }
                            if (cities[b].outOfBoundsCount > PUSHED_OUT_OF_BOUNDS_MAX)
                            {
                                cities.RemoveAt(b);
                                notPlaceableCities += 1;
                                skip = true;
                            }

                        }
                    }
                }

                if (!smthMoved)
                    break;
            }

            //remove those rects that are at least half water
            cities.RemoveAll((info) => {
                int waterCount = 0;
                for (int x = info.rect.X; x < info.rect.X + info.rect.Width; x++)
                {
                    for (int y = info.rect.Y; y < info.rect.Y + info.rect.Height; y++)
                    {
                        if (tiles[x, y].type == TileType.Water)
                            waterCount += 1;
                    }
                }
                return waterCount > (info.rect.Width * info.rect.Height) / 2;
            });

            return cities;
        }

        /// <summary>
        /// Move the referenced rectangle into the bounds of the map.
        /// </summary>
        /// <param name="rect">A reference to the rectangle to be moved inside the map bounds.</param>
        private void MoveIntoBounds(ref Rectangle rect)
        {
            if (rect.X < 0)
                rect.X = 4;
            if (rect.Right >= param.size.X)
                rect.X = param.size.X - rect.Size.X - 4;
            if (rect.Y < 0)
                rect.Y = 4;
            if (rect.Y + rect.Size.Y >= param.size.Y)
                rect.Y = param.size.Y - rect.Size.Y - 4;
        }

        /// <summary>
        /// Check if a rectangle is out of the bounds of the map.
        /// </summary>
        /// <param name="r">The rectangle to be checked.</param>
        /// <returns>Boolean indicating if the rectangle is out of bounds.</returns>
        private bool IsOutOfBounds(Rectangle r)
        {
            Point p = r.Location;
            if (p.X < 0 || p.X >= param.size.X || p.Y < 0 || p.Y >= param.size.Y)
                return true;
            p = r.Location + r.Size;
            if (p.X < 0 || p.X >= param.size.X || p.Y < 0 || p.Y >= param.size.Y)
                return true;
            return false;
        }

        #endregion        

    }

    #region Helping Data Structures

    struct DistrictFillInfo
    {
        public DistrictType citizenLevel;
        public Point startPoint;

        public DistrictFillInfo(Point p, DistrictType level)
        {
            startPoint = p;
            citizenLevel = level;
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
            Debug.Assert(GeneratorHelper.tiles[pos.X, pos.Y].GetSlopeIndex() == 0);
            directions = 0;
            if (random.NextDouble() < dirProbs[0] && CanMove(Direction.Up))
                directions += 1;
            if (random.NextDouble() < dirProbs[1] && CanMove(Direction.Right))
                directions += 2;
            if (random.NextDouble() < dirProbs[2] && CanMove(Direction.Down))
                directions += 4;
            if (random.NextDouble() < dirProbs[3] && CanMove(Direction.Left))
                directions += 8;

        }

        bool CanMove(Direction dir)
        {
            int slope = 0;
            switch (dir)
            {
                case Direction.Up:
                    if (!IsInRange(pos.X, pos.Y - 1))
                        return false;
                    slope = GeneratorHelper.tiles[pos.X, pos.Y - 1].GetSlopeIndex();
                    return slope == 0 || slope == 12;                    
                case Direction.Right:
                    if (!IsInRange(pos.X + 1, pos.Y))
                        return false;
                    slope = GeneratorHelper.tiles[pos.X + 1, pos.Y].GetSlopeIndex();
                    return slope == 0 || slope == 9;
                case Direction.Down:
                    if (!IsInRange(pos.X, pos.Y + 1))
                        return false;
                    slope = GeneratorHelper.tiles[pos.X, pos.Y + 1].GetSlopeIndex();
                    return slope == 0 || slope == 3;
                case Direction.Left:
                    if (!IsInRange(pos.X - 1, pos.Y))
                        return false;
                    slope = GeneratorHelper.tiles[pos.X - 1, pos.Y].GetSlopeIndex();
                    return slope == 0 || slope == 6;
            }
            return true;
        }

        public bool HasDirection(Direction dir)
        {
            return (dir & directions) > 0;
        }

    }

    class CityPlacementInfo
    {
        public Rectangle rect;
        public int size;
        public int outOfBoundsCount;

        public CityPlacementInfo(Rectangle rect, int size)
        {
            this.rect = rect;
            this.size = size;
            outOfBoundsCount = 0;
        }

        public Point GetCityCenter()
        {
            return rect.Center;
        }
    }

    #endregion
}
