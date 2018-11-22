using Industry.World;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Industry.Simulation
{
    public class City
    {
        public static int cityID = 1;

        public HashSet<Point> buildingTiles;
        public Point MiddlePoint { get; private set; }
        public int Citizens { get { return buildingTiles.Count; } }
        public string Name { get; private set; }

        public Dictionary<Point, Store> stores;

        private int[] citizenLevels;

        public City(Tile[,] tiles, World.Generation.Room room)
        {
            buildingTiles = new HashSet<Point>();
            stores = new Dictionary<Point, Store>();

            citizenLevels = new int[(int)DistrictType.Business];

            foreach (Point p in room.Tiles)
            {
                Tile t = tiles[p.X, p.Y];
                if (t.type == TileType.House)
                {
                    buildingTiles.Add(p);
                    citizenLevels[(int)t.citizenLevel - 1]++;
                }
                t.city = this;
            }

            Debug.WriteLine($"City buildings: {buildingTiles.Count}");
            Debug.WriteLine($"Citizen Levels: {citizenLevels[0]}, {citizenLevels[1]}, {citizenLevels[2]}.");
            MiddlePoint = room.MiddlePoint;
            Name = $"City {cityID++}";
        }

        public void PlaceStore(Store store)
        {
            Point p = store.tilePosition;
            Debug.Assert(buildingTiles.Contains(p));
            stores.Add(p, store);
            buildingTiles.Remove(p);
        }

        public int GetNumberOfCitizensOfLevel(DistrictType lvl)
        {
            return citizenLevels[(int)lvl - 1];
        }

    }   

    public enum TrafficLevel
    {
        Low,
        Mid,
        High
    }

}
