using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Industry.World.Generation.GenHelper;


namespace Industry.World.Generation.Modules
{
    public class CleanUpModule : IGeneratorModule
    {

        private List<Room> cities;
        private List<Room> waters;

        public CleanUpModule(List<Room> cities, List<Room> waters)
        {
            this.cities = cities;
            this.waters = waters;
        }

        public void Apply(GeneratorParameter param, Tile[,] tiles)
        {
            CalculateCorrectRoadTile(param, tiles);

            SetRoomsToTile(tiles);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tiles"></param>
        private void SetRoomsToTile(Tile[,] tiles)
        {
            foreach (Room room in cities)
            {
                foreach (Point p in room.Tiles)
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

        /// <summary>
        /// Auto tiling the onTopIndex for the road tiles. Based on the four directional neighbour tiles calculate the roadDir index.
        /// </summary>
        private void CalculateCorrectRoadTile(GeneratorParameter param, Tile[,] tiles)
        {
            //classic auto tile by giving the directional neighbour roads power of two values, result is a road dir index between 0 and 15.
            for (int x = 0; x < param.size.X; x++)
            {
                for (int y = 0; y < param.size.Y; y++)
                {
                    Tile t = tiles[x, y];

                    if (t.type != TileType.Bridge && t.type == TileType.Road)
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
                        if (!(slope == 6 && roadDir == 10 ||
                                slope == 12 && roadDir == 5 ||
                                slope == 9 && roadDir == 10 ||
                                slope == 3 && roadDir == 5))
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
                            tiles[x, y].onTopIndex = changeTo[roadDir];
                        }
                    }
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

    }
}
