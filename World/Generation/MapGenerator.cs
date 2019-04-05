using Barely.ProgGen;
using Barely.Util.Priority_Queue;
using Industry.Simulation;
using Industry.World.Generation.Modules;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

            City.cityID = 1;
            this.tileset = param.tileset;
            random = new Random(param.randomSeed);
            Debug.WriteLine($"{param.randomSeed}");
            tiles = new Tile[param.size.X, param.size.Y];
            GeneratorHelper.Size = param.size;
            GeneratorHelper.tiles = tiles;
            cities.Clear();
            waters.Clear();

            modules.Add(new TerrainModule());

            if (param.hasRivers)
                modules.Add(new RiverModule(waters, random));
            if (param.hasCities)
                modules.Add(new CityModule(cities, random));
            if (param.hasCityConnections)
                modules.Add(new CityConnectionModule(cities, random));
            if(param.forestSize > 0.0f)
                modules.Add(new ForestModule(random));
            if (param.resourceSize > 0.0f)
                modules.Add(new ResourceModule(random));

            modules.Add(new CleanUpModule(cities, waters));
        }

        public Tile[,] Generate()
        {
            
            foreach(IGeneratorModule module in modules)
            {
                module.Apply(param, tiles);
            }
                                           
            return tiles;
        }           

    }
    
}
