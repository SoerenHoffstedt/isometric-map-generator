using Barely.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Barely.ProgGen;
using Industry.World.Generation;
using BarelyUI;
using Glide;
using Industry.Agents;
using Industry.UI;
using Industry.Scenes;
using Industry.Simulation;

namespace Industry.World
{
    public class Map
    {
        public enum PlacementState
        {
            Nothing,
            PlaceRoad
        }

        PlacementState state;
        
        public Tile[,] tiles;
        Point mapSize;        
        Point tileSize = new Point(64, 32);
        int tileDirtHeight = 16;
        
        public List<Room> cityRooms { get; private set; }

        Pathfinder pathfinder;
        GeneratorParameter parameter;
        Point resolution;        

        RasterizerState rasterizerState;
        Tweener tweener = new Tweener();
        Camera camera;

        Point mouseOverCoord;        

        public Map(GeneratorParameter mapParameter, Camera camera, ContentManager Content, GraphicsDevice GraphicsDevice, Point resolution)
        {
            this.camera = camera;            
            this.resolution = resolution;          
            rasterizerState = new RasterizerState();
            rasterizerState.ScissorTestEnable = true;
            GenerateMap(mapParameter);
        }        

        public void Update(float deltaTime)
        {            
            tweener.Update(deltaTime);                             
        }        

        public void GenerateMap(GeneratorParameter param)
        {            
            MapGenerator generator = new MapGenerator(param);
            Tile[,] ts = generator.Generate();
            tiles = ts;
            cityRooms = generator.cities;
            this.parameter = param;
            this.mapSize = param.size;
            pathfinder = new Pathfinder(tiles);
        }
        
        public List<Point> GetPathForAgent(Agent agent, Point to)
        {
            if(Tile(to).type != TileType.Road)
            {
                foreach(Point n in IterateNeighboursFourDir(to.X, to.Y))
                {
                    if(Tile(n).type == TileType.Road)
                    {
                        to = n;
                        break;
                    }
                }
            }

            Point from = agent.tilePosition;
            if(Tile(from).type != TileType.Road)
            {
                foreach (Point n in IterateNeighboursFourDir(from.X, from.Y))
                {
                    if (Tile(n).type == TileType.Road)
                    {
                        from = n;
                        break;
                    }
                }
            }

            //Debug.Assert(Tile(to).type == TileType.Road, "Tile does not have a road neighbour. No delivery possible");
            //Debug.Assert(Tile(from).type == TileType.Road, "Tile does not have a road neighbour. No delivery possible");
            //TODO: do the above asserts instead, but right now the map is not guaranteed to be all road connected so they would fuck everything up.
            if (Tile(to).type != TileType.Road || Tile(from).type != TileType.Road)
                return null;

            List<Point> path = pathfinder.PathFromTo(from, to);
            if(path != null && path.Count > 0)
                agent.tilePosition = from;
            return path;            
        }

        public void PlacePizzaStore(Point p, int spriteIndex, Store store)
        {
            Tile(p).type = TileType.Pizza;
            Tile(p).onTopIndex = spriteIndex;
            Tile(p).store = store;
            Tile(p).city.PlaceStore(store);
        }

        public Tile GetMouseOverTile()
        {
            mouseOverCoord = CalculateTilePosition(Input.GetMousePosition());
            return IsInRange(mouseOverCoord) ? Tile(mouseOverCoord) : null;
        }

        #region Renderer API

        public Point GetMapSize()
        {
            return mapSize;
        }

        public int GetMaxHeight()
        {
            return parameter.maxHeight;
        }

        public int GetMinHeight()
        {
            return parameter.minHeight;
        }        

        public int GetTileDirtHeight()
        {
            return tileDirtHeight;
        }

        public Point GetTileSize()
        {
            return tileSize;
        }

        public Tile GetTile(int x, int y)
        {
            Debug.Assert(IsInRange(x, y));
            return tiles[x, y];
        }

        public Tile GetTile(Point p)
        {
            Debug.Assert(IsInRange(p));
            return Tile(p);
        }

        public Tile this[int x, int y]
        {
            get { return IsInRange(x, y) ? tiles[x, y] : null; }
        }

        public Tile this[Point p]
        {
            get { return IsInRange(p) ? tiles[p.X, p.Y] : null; }
        }

        #endregion

        #region Calculations

        private Point CalculateTilePosition(Point screenPos)
        {
            bool HasHeight(Tile t, int h)
            {
                for (int i = 0; i < t.height.Length; i++)
                {
                    if (t.height[i] == h)
                        return true;
                }
                return false;
            }
            /*
             i have a max height
             the height difference of a tile is tileDirtHeight = 16
             Calc the mouse tile from thje mouse position with added height and see what tile it would be.
             the last tile that existed on that height is the selected
             */
            Point mousePosition = new Point(-1, -1);
            Vector2 originalPos = camera.ToWorld(screenPos).ToVector2();
            originalPos.X -= tileSize.X / 2;

            for (int i = 1; i <= parameter.maxHeight + 1; i++)
            {
                Vector2 realMousePosition = originalPos;
                realMousePosition.Y += i * tileDirtHeight;

                int x = (int)(realMousePosition.X / (tileSize.X / 2) + realMousePosition.Y / (tileSize.Y / 2)) / 2;
                int y = (int)((realMousePosition.Y / (tileSize.Y / 2) - (realMousePosition.X / (tileSize.X / 2))) / 2);

                if (IsInRange(x, y) && tiles[x, y].GetMaxHeight() == i)
                {
                    mousePosition = new Point(x, y);
                }

            }

            return mousePosition;
        }

        public Point GetPositionByCoords(Point c)
        {
            return GetPositionByCoords(c.X, c.Y);
        }

        public Point GetPositionByCoords(int x, int y)
        {
            return new Point((x - y) * tileSize.X / 2, (x + y) * tileSize.Y / 2);
        }

        #endregion

        #region Helper

        private Tile Tile(Point p)
        {
            return tiles[p.X, p.Y];
        }       

        public bool IsInRange(int x, int y)
        {
            return x >= 0 && y >= 0 && x < mapSize.X && y < mapSize.Y;
        }

        public bool IsInRange(Point p)
        {
            return IsInRange(p.X, p.Y);
        }

        public IEnumerable<Point> IterateNeighboursFourDir(int x, int y)
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

        private IEnumerable<Point> IterateNeighboursEightDir(int x, int y)
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

        private IEnumerable<Point> IterateCornerNeighbours(int x, int y)
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

        #endregion
    }
}


