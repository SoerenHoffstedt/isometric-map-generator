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
            GenHelper.Size = param.size;
            this.tileset = param.tileset;
            random = new Random(param.randomSeed);
            Debug.WriteLine($"{param.randomSeed}");
            tiles = new Tile[param.size.X, param.size.Y];
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

        private void CalculateCorrectRoadTile()
        {
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
                            t.type = TileType.Forest;
                            Debug.WriteLine($"Road piece not existing. Slope: {slope}, roadDir: {roadDir}");
                            continue;
                        }                                           
                    }


                    t.onTopIndex = roadDir;
                }
            }
        }       

    }
    
}
