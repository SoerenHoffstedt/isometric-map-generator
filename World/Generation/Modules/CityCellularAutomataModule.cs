using Industry.Simulation;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static Industry.World.Generation.GenHelper;

namespace Industry.World.Generation.Modules
{
    public class CityCellularAutomataModule : IGeneratorModule
    {
        private List<Room> cities;
        private Random random;

        public CityCellularAutomataModule(List<Room> cities, Random random)
        {
            cities = cities;
            this.random = random;
        }

        public void Apply(GeneratorParameter param, Tile[,] tiles)
        {
            CreateCitiesCellularAutomata(param, tiles);
            ConnectRoadArtifacts(param, tiles);
            ConnectRoadArtifacts(param, tiles);
        }

        private void CreateCitiesCellularAutomata(GeneratorParameter param, Tile[,] tiles)
        {
            var rooms = GenHelper.GetCellularAutomataAsRooms(6, 53, true);
            HashSet<Room> toRemove = new HashSet<Room>();
            int count = 7;

            foreach (Room room in rooms)
            {
                int size = room.Tiles.Count;
                if (size < 15)
                {
                    toRemove.Add(room);
                    continue;
                }

                int modX = 3;
                int modY = 3;
                if (random.NextDouble() < 0.5)
                    modX = random.Next(3, 6);
                else
                    modY = random.Next(3, 6);

                cities.Add(room);
                count += 7;

                double levelThree = GetPercentageOfCitizenLevel(CitizenLevel.Three);
                double levelOne = GetPercentageOfCitizenLevel(CitizenLevel.One);

                foreach (Point p in room.Tiles)
                {
                    Tile t = tiles[p.X, p.Y];

                    double prob = random.NextDouble();
                    CitizenLevel lvl = CitizenLevel.None;
                    if (prob <= levelOne)
                        lvl = CitizenLevel.One;
                    else if (prob <= levelOne + levelThree)
                        lvl = CitizenLevel.Three;
                    else
                        lvl = CitizenLevel.Two;

                    if (t.type == TileType.Nothing && t.AllHeightsAreSame())
                    {
                        if ((p.X % modX) == 0 || (p.Y % modY) == 0)
                        {
                            t.type = TileType.Road;
                        }
                        else
                        {
                            t.SetCitizenLevel(lvl);
                            t.type = TileType.House;
                            t.onTopIndex = param.tileset.GetRandomHouseIndex(t.citizenLevel);
                        }
                    }
                }
                room.Tiles.RemoveWhere((p) => tiles[p.X, p.Y].type == TileType.Water || tiles[p.X, p.Y].type == TileType.Forest);

                if (room.Tiles.Count < 15)
                {
                    toRemove.Add(room);
                    continue;
                }

            }

            cities.RemoveAll((r) => toRemove.Contains(r));            
        }

        const int CITY_PART_MIN_SIZE = 15;

        private void ConnectRoadArtifacts(GeneratorParameter param, Tile[,] tiles)
        {
            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    Tile t = tiles[x, y];

                    if (t.type != TileType.Nothing || !t.IsRoadPlaceable())
                        continue;

                    int roadNeighbours = 0;
                    bool wasLastRoad = IsInRange(x - 1, y) && tiles[x - 1, y].type == TileType.Road;
                    bool loops = false;

                    if (IsInRange(x, y - 1) && tiles[x, y - 1].type == TileType.Road)
                    {
                        roadNeighbours++;
                        if (wasLastRoad && IsInRange(x - 1, y - 1) && tiles[x - 1, y - 1].type == TileType.Road)
                            loops = true;
                        wasLastRoad = true;
                    }
                    else
                        wasLastRoad = false;

                    if (IsInRange(x + 1, y) && tiles[x + 1, y].type == TileType.Road)
                    {
                        roadNeighbours++;
                        if (wasLastRoad && IsInRange(x + 1, y - 1) && tiles[x + 1, y - 1].type == TileType.Road)
                            loops = true;
                        wasLastRoad = true;
                    }
                    else
                        wasLastRoad = false;

                    if (IsInRange(x, y + 1) && tiles[x, y + 1].type == TileType.Road)
                    {
                        roadNeighbours++;
                        if (wasLastRoad && IsInRange(x + 1, y + 1) && tiles[x + 1, y + 1].type == TileType.Road)
                            loops = true;
                        wasLastRoad = true;
                    }
                    else
                        wasLastRoad = false;

                    if (IsInRange(x - 1, y) && tiles[x - 1, y].type == TileType.Road)
                    {
                        roadNeighbours++;
                        if (wasLastRoad && IsInRange(x - 1, y + 1) && tiles[x - 1, y + 1].type == TileType.Road)
                            loops = true;
                        wasLastRoad = true;
                    }
                    else
                        wasLastRoad = false;


                    if (roadNeighbours > 1 && !loops)
                    {
                        t.type = TileType.Road;
                        t.onTopIndex = 0;
                        //t.color = Color.Aquamarine;                       
                    }
                }
            }
        }

        const double levelPlusMinus = 0.1;
        const double levelThreePer = 0.5;
        const double levelOnePer = 0.2;

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

    }
}
