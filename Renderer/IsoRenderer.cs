using Barely.Util;
using BarelyUI;
using Industry.Agents;
using Industry.Simulation;
using Industry.World;
using Industry.World.Generation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.Renderer
{
    public class IsoRenderer
    {
        private int tileDirtHeight;
        private Point tileSize;
        private Tileset tileset;
        private Point mapSize;
        private SpriteFont cityFont;
        private float maxDepth;
        private RasterizerState uiRasterizerState;

        public IsoRenderer(Map map, Tileset tileset, SpriteFont cityFont)
        {
            this.tileset        = tileset;            
            this.cityFont       = cityFont;
            uiRasterizerState = new RasterizerState();
            uiRasterizerState.ScissorTestEnable = true;
            ResetMap(map);
        }

        public void ResetMap(Map map)
        {
            tileDirtHeight = map.GetTileDirtHeight();
            tileSize = map.GetTileSize();
            mapSize = map.GetMapSize();
            Point max = GetPositionByCoords(mapSize);
            maxDepth = (float)(max.X + max.Y) * (float)SortingLayer.Count;
        }

        public void Draw(SpriteBatch spriteBatch, 
                         Camera camera, 
                         Map map, 
                         HashSet<Agent> agents, 
                         List<City> cities, 
                         PlacementPreviewData previewData,
                         List<HighlightTileRenderData> highlightData,
                         Tile mouseOverTile,
                         Canvas uiCanvas,
                         bool hideUI)
        {

            int spriteCount = 0;

            Point resolution = Config.Resolution;
            SamplerState samplerState = camera.zoom < 1.0f ? SamplerState.LinearWrap : SamplerState.PointWrap;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState, transformMatrix: camera.Transform);

            Point MouseToWorldCoord(Point screenPos)
            {
                Point p = new Point(-1, -1);
                Vector2 originalPos = camera.ToWorld(screenPos).ToVector2();

                for (int i = 1; i <= map.GetMaxHeight() + 1; i++)
                {
                    Vector2 realMousePosition = originalPos;
                    realMousePosition.Y += i * tileDirtHeight;

                    int x = (int)(realMousePosition.X / (tileSize.X / 2) + realMousePosition.Y / (tileSize.Y / 2)) / 2;
                    int y = (int)((realMousePosition.Y / (tileSize.Y / 2) - (realMousePosition.X / (tileSize.X / 2))) / 2);
                    if (map.IsInRange(x, y) && map[x, y].GetMaxHeight() == i)
                    {
                        p = new Point(x, y);
                    }
                }

                if (p == new Point(-1, -1))
                {
                    Vector2 mp = originalPos;
                    mp.Y += map.GetMinHeight() * tileDirtHeight;
                    int x = (int)(mp.X / (tileSize.X / 2) + mp.Y / (tileSize.Y / 2)) / 2;
                    int y = (int)((mp.Y / (tileSize.Y / 2) - (mp.X / (tileSize.X / 2))) / 2);
                    p = new Point(x, y);
                }

                return p;
            }

            Point ClampPointToMap(Point p)
            {
                return new Point(MathHelper.Clamp(p.X, 0, mapSize.X - 1), MathHelper.Clamp(p.Y, 0, mapSize.Y - 1));
            }

            Point p1 = MouseToWorldCoord(new Point(0, 0));
            Point p2 = MouseToWorldCoord(resolution - new Point(0, 0));
            Point p3 = MouseToWorldCoord(new Point(0, resolution.Y));
            Point p4 = MouseToWorldCoord(new Point(resolution.X, 0));

            Point from = new Point(Min(p1.X, p2.X, p3.X, p4.X) - 1, Min(p1.Y, p2.Y, p3.Y, p4.Y) - 1);
            Point to = new Point(Max(p1.X, p2.X, p3.X, p4.X) + 1, Max(p1.Y, p2.Y, p3.Y, p4.Y) + 1);
            from = ClampPointToMap(from);
            to = ClampPointToMap(to);

            Sprite baseSprite = tileset.GetLandscapeSprite(TileType.Nothing, 0);

            for(int x = 0; x < mapSize.X; x++)
            {
                int y = mapSize.Y - 1;
                int height = map[x, y].GetMaxHeight();
                Point pos = GetPositionByCoords(x, y);
                baseSprite.Render(spriteBatch, pos);
                spriteCount++;
                for (int i = 1; i < height; i++)
                {
                    Point off = new Point(0, -i * tileDirtHeight);
                    baseSprite.Render(spriteBatch, pos + off);
                    spriteCount++;
                }
            }

            for (int y = 0; y < mapSize.Y; y++)
            {
                int x = mapSize.X - 1;                
                int height = map[x, y].GetMaxHeight();
                Point pos = GetPositionByCoords(x, y);
                baseSprite.Render(spriteBatch, pos);
                spriteCount++;
                for (int i = 1; i < height; i++)
                {
                    Point off = new Point(0, -i * tileDirtHeight);
                    baseSprite.Render(spriteBatch, pos + off);
                    spriteCount++;
                }
            }

            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, samplerState, transformMatrix: camera.Transform);

            for (int x = from.X; x <= to.X; x++)
            {
                for (int y = from.Y; y <= to.Y; y++)
                {
                    Tile t = map[x, y];
                    int slope = t.GetSlopeIndex();
                    int height = t.GetMaxHeight();
                    Point pos = GetPositionByCoords(x, y);

                    if (height == 0)
                        continue;

                    bool isWater = t.type == TileType.Water;

                    Sprite sprite = tileset.GetLandscapeSprite(isWater ? TileType.Water : TileType.Nothing, slope);                  

                    Point offset = new Point(0, -height * tileDirtHeight);
                    if (t.type == TileType.Water)
                        offset.Y += 4;

                    Color col = map[x, y].color;
                    if (t == mouseOverTile)
                        col = Color.Red;                   

                    if (t.type == TileType.Road)
                    {
                        Sprite roadSprite = tileset.GetRoadSprite(t.onTopIndex, slope);
                        roadSprite.Render(spriteBatch, pos + offset, GetDepth(new Point(x, y), SortingLayer.Map), col);
                    }
                    else
                        sprite.Render(spriteBatch, pos + offset, GetDepth(new Point(x, y), SortingLayer.Map), col);

                    spriteCount++;

                }
            }

            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, samplerState, transformMatrix: camera.Transform);


            Dictionary<Point, List<RenderData>> toDraw = new Dictionary<Point, List<RenderData>>();

            foreach (Agent a in agents)
            {
                if (a.state == AgentState.Idle || a.state == AgentState.Pause)
                    continue;
                int height = map[a.tilePosition].GetMaxHeight() * tileDirtHeight;
                Vector2 vec = CalculateTilePositionFromWorld(a.GetRenderPosition(), map);
                float depth = GetDepth(a.tilePosition, SortingLayer.Vehicles); //((vec.X + vec.Y) * (float)SortingLayer.Count + (float)SortingLayer.Vehicles) / maxDepth;
                Point pos = a.GetRenderPosition() - new Point(0, height); // + new Point(tileSize.X / 4, 0);
                Sprite sprite = a.GetSprite();
                if (sprite.Size.Y > 32)
                    pos.Y -= sprite.Size.Y - 32;

                if (!toDraw.ContainsKey(a.tilePosition))
                    toDraw.Add(a.tilePosition, new List<RenderData>());
                toDraw[a.tilePosition].Add(new RenderData(sprite, pos, depth));
            }

            bool IsDrawnOnTop(TileType type)
            {
                return type == TileType.House || type == TileType.Forest || type == TileType.Pizza || type == TileType.Bridge;
            }

            for (int x = from.X; x <= to.X; x++)
            {
                for (int y = from.Y; y <= to.Y; y++)
                {
                    Point p = new Point(x, y);
                    Tile tile = map[x, y];

                    Color col = Color.White;
                    Sprite sprite = null;
                    Point pos = Point.Zero;
                    float depth = 0f;

                    if (toDraw.ContainsKey(p))
                    {
                        foreach (RenderData data in toDraw[p])
                        {
                            pos = data.pos;
                            sprite = data.sprite;
                            depth = data.depth;
                            sprite.Render(spriteBatch, pos, depth, Color.White);
                            spriteCount++;
                        }
                        continue;
                    }
                    else if (IsDrawnOnTop(tile.type))
                    {
                        if (tile.type == TileType.House)
                            sprite = tileset.GetHouseSprite(tile.citizenLevel, tile.onTopIndex);
                        else
                            sprite = tileset.GetOnTopSprite(tile.type, tile.onTopIndex);
                        Point position = GetPositionByCoords(x, y);
                        int height = tile.GetMaxHeight();
                        Point offset = new Point(0, -height * tileDirtHeight);
                        int spriteHeight = sprite.spriteRect.Height;
                        if (spriteHeight > tileSize.Y)
                            offset.Y -= spriteHeight - tileDirtHeight - tileSize.Y;
                        offset.Y -= tileDirtHeight;
                        pos = position + offset;
                        depth = GetDepth(new Point(x, y), SortingLayer.Vehicles);
                    }

                    if (tile == mouseOverTile)
                    {
                        if (previewData != null)
                        {
                            sprite = tileset.GetOnTopSprite(previewData.type, previewData.spriteIndex);
                            Point position = GetPositionByCoords(x, y);
                            int height = tile.GetMaxHeight();
                            Point offset = new Point(0, -height * tileDirtHeight);
                            int spriteHeight = sprite.spriteRect.Height;
                            if (spriteHeight > tileSize.Y)
                                offset.Y -= spriteHeight - tileDirtHeight - tileSize.Y;
                            offset.Y -= tileDirtHeight;
                            pos = position + offset;

                            if (previewData.type == TileType.Pizza && mouseOverTile.type == TileType.House)
                                col = Color.Green;
                            else
                                col = Color.Red;
                        }
                        else
                            col = Color.Yellow;

                    }
                    if (sprite != null)
                    {
                        sprite.Render(spriteBatch, pos, depth, col);
                        spriteCount++;
                    }

                }
            }

            Effects.Render(spriteBatch);

            int c = 0;
            foreach (City city in cities)
            {
                Point p = city.MiddlePoint;
                p = GetPositionByCoords(p) - new Point(0, tileSize.Y);
                spriteBatch.DrawString(cityFont, city.Name, p.ToVector2(), Color.Yellow);
                c++;
            }


            foreach(HighlightTileRenderData data in highlightData)
            {
                Point pos       = GetPositionByCoords(data.coordinate);
                Point p         = data.coordinate;
                int height      = map[p.X, p.Y].GetMaxHeight();
                Point offset    = new Point(0, -height * tileDirtHeight);

                Sprite sprite   = tileset.GetHightlightSprite(data.spriteId);

                offset.X = (tileSize.X - sprite.spriteRect.Width) / 2;
                offset.Y -= data.yOffset;

                sprite.Render(spriteBatch, pos + offset, data.color);
                spriteCount++;
            }

            spriteBatch.End();


            //Debug.WriteLine($"Sprite Drawn: {spriteCount}");       
            if (!hideUI)
            {
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, rasterizerState: uiRasterizerState, transformMatrix: camera.uiTransform);
                uiCanvas.Render(spriteBatch);        
                spriteBatch.End();
            }
            
        }

        #region Calculation

        private float GetDepth(Point p, SortingLayer layer)
        {
            Debug.Assert(layer != SortingLayer.Count);

            float k = (float)p.X + p.Y;
            return (k * (float)SortingLayer.Count + (float)layer) / maxDepth;
        }

        public Point GetPositionByCoords(Point c)
        {
            return GetPositionByCoords(c.X, c.Y);
        }

        public Point GetPositionByCoords(int x, int y)
        {
            return new Point((x - y) * tileSize.X / 2, (x + y) * tileSize.Y / 2);
        }

        private Vector2 CalculateTilePositionFromWorld(Point worldPos, Map map)
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
            Vector2 mousePosition = new Vector2(-1, -1);
            Vector2 originalPos = worldPos.ToVector2();
            originalPos.X -= (float)tileSize.X / 2f;

            for (int i = 1; i <= map.GetMaxHeight() + 1; i++)
            {
                Vector2 realMousePosition = originalPos;
                realMousePosition.Y += (float)i * tileDirtHeight;

                float x = (realMousePosition.X / (tileSize.X / 2f) + realMousePosition.Y / (tileSize.Y / 2f)) / 2f;

                float y = ((realMousePosition.Y / (tileSize.Y / 2f) - (realMousePosition.X / (tileSize.X / 2f))) / 2f);

                if (map.IsInRange((int)x, (int)y) && map[(int)x, (int)y].GetMaxHeight() == i)
                {
                    mousePosition = new Vector2(x, y);
                }

            }

            return mousePosition;
        }

        #endregion

        #region Helper

        private int Min(params int[] values)
        {
            int m = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] < m)
                    m = values[i];
            }
            return m;
        }

        private int Max(params int[] values)
        {
            int m = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > m)
                    m = values[i];
            }
            return m;
        }

        #endregion

    }

}
