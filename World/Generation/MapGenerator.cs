using Barely.ProgGen;
using Barely.Util.Priority_Queue;
using Industry.Simulation;
using Industry.World.Generation.Modules;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static Industry.World.Generation.GenHelper;

namespace Industry.World.Generation
{
    public class MapGenerator
    {
        private Random random;        
        private Tile[,] tiles;
        private Tileset tileset;
        public List<Room> cities;
        public List<Room> waters;              
        private GeneratorParameter param;
        private List<IGeneratorModule> modules = new List<IGeneratorModule>(10);

        public MapGenerator(GeneratorParameter param)
        {
            this.param = param;
            cities = new List<Room>(64);
            waters = new List<Room>(32);
        }

        public Tile[,] Generate()
        {
            City.cityID = 1;                       
            this.tileset = param.tileset;
            random = new Random(param.randomSeed);
            Debug.WriteLine($"{param.randomSeed}");
            tiles = new Tile[param.size.X, param.size.Y];
            GenHelper.Size = param.size;
            GenHelper.tiles = tiles;
            cities.Clear();
            waters.Clear();           
            
            modules.Add(new TerrainModule());            
            if(param.hasRivers)
                modules.Add(new RiverModule(waters, random));
            if(param.hasCities)
                modules.Add(new CityModule(cities, random));
            if(param.hasCityConnections)
                modules.Add(new CityConnectionModule(cities, random));
            modules.Add(new ForestModule());

            foreach(IGeneratorModule module in modules)
            {
                module.Apply(param, tiles);
            }
                                
            CalculateCorrectRoadTile();

            SetRoomsToTile();

            GenHelper.tiles = null;

            return tiles;
        }

        private void SetRoomsToTile()
        {
            foreach(Room room in cities)
            {
                foreach(Point p in room.Tiles)
                {
                    tiles[p.X, p.Y].partOfRoom = room;
                }
            }

            foreach (Room room in waters)
            {
                foreach (Point p in room.Tiles)
                {
                    tiles[p.X, p.Y].partOfRoom = room;
                }
            }

        }

        //Dictionary to indicated to which tile diagonals are supposed to be connected.
        Dictionary<int, int> connectedTo = new Dictionary<int, int>()
                    {
                        { 9,  6 },
                        { 6,  9 },
                        { 3, 12 },
                        { 12, 3 }
                    };

        //Dictionary translating the diagonal road directions to the special indexes of the actual diagonals.
        Dictionary<int, int> changeTo = new Dictionary<int, int>()
                    {
                        { 3, 16 },
                        { 6, 17 },
                        { 9, 18 },
                        {12, 19 }
                    };


        /// <summary>
        /// Auto tiling the onTopIndex for the road tiles. Based on the four directional neighbour tiles calculate the roadDir index.
        /// </summary>
        private void CalculateCorrectRoadTile()
        {
            //classic auto tile by giving the directional neighbour roads power of two values, result is a road dir index between 0 and 15.
            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    Tile t = tiles[x, y];
                    if (t.type != TileType.Road)
                        continue;                    

                    int roadDir = 0;

                    if (IsInRange(x - 1, y) && tiles[x - 1, y].type == TileType.Road)
                        roadDir += 8;

                    if (IsInRange(x + 1, y) && tiles[x + 1, y].type == TileType.Road)
                        roadDir += 2;

                    if (IsInRange(x, y - 1) && tiles[x, y - 1].type == TileType.Road)
                        roadDir += 1;

                    if (IsInRange(x, y + 1) && tiles[x, y + 1].type == TileType.Road)
                        roadDir += 4;

                    int slope = t.GetSlopeIndex();
                    if (slope != 0)
                    {
                        if(!(   slope == 6  && roadDir == 10 ||
                                slope == 12 && roadDir == 5  ||
                                slope == 9  && roadDir == 10 ||
                                slope == 3  && roadDir == 5  ))
                        {
                            //on these slopes roads can not be placed. And it should not happen in the first place.
                            t.type = TileType.Nothing;
                            continue;
                        }                                           
                    }                                     

                    t.onTopIndex = roadDir;
                }
            }

            //Special case: check if a curve tile could be a diagonal tile.
            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    if (tiles[x, y].type != TileType.Road)
                        continue;

                    int roadDir = tiles[x, y].onTopIndex;
                    
                    if (roadDir == 9 || roadDir == 6 || roadDir == 3 || roadDir == 12)
                    {                        
                        int expectedConnectionRoadDir1 = connectedTo[roadDir];
                        int expectedConnectionRoadDir2 = changeTo[expectedConnectionRoadDir1];
                        int count = 0;
                        foreach (Point p in GenHelper.IterateNeighboursFourDir(x, y))
                        {
                            if (tiles[p.X, p.Y].type == TileType.Road && 
                                (tiles[p.X, p.Y].onTopIndex == expectedConnectionRoadDir1 || 
                                tiles[p.X, p.Y].onTopIndex == expectedConnectionRoadDir2))
                            {
                                count += 1;                                
                            }
                        }
                        //if one or two tiles with expected road dir, change the road.
                        if (count == 1 || count == 2)
                        {
                            tiles[x,y].onTopIndex = changeTo[roadDir];
                        }
                    }
                }
            }

        }       

    }
    
}
