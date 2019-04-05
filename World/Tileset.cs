using Barely.Util;
using Industry.Simulation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Industry.World
{
    public class Tileset
    {
        Random random = new Random();
        string name;
        
        private Sprite[][] sprites;
        private Dictionary<Tuple<int,int>, Sprite> roadSprites;
        private Dictionary<Tuple<int, int>, Sprite> bridgeSprites;
        private Sprite[] pizzaBuildings;
        private Sprite[][] houseBuildings;
        private Dictionary<string, Sprite> hightlightSprites;
    
        public Tileset(XmlNode def, ContentManager Content)
        {
            XmlNodeList tiles = def.SelectNodes("normal/t");
            Texture2D tex = Content.Load<Texture2D>(def.Attributes["atlas"].Value);

            sprites = new Sprite[(int)TileType.Count][];

            name = def.Attributes["name"].Value;
            sprites[(int)TileType.Nothing] = new Sprite[16];            

            foreach (XmlNode t in tiles)
            {
                int slopeIndex = int.Parse(t.Attributes["slope"].Value);
                int x = int.Parse(t.Attributes["x"].Value);
                int y = int.Parse(t.Attributes["y"].Value);
                int w = int.Parse(t.Attributes["w"].Value);
                int h = int.Parse(t.Attributes["h"].Value);
                sprites[(int)TileType.Nothing][slopeIndex] = new Sprite(tex, new Rectangle(x, y, w, h), Color.White);
            }

            sprites[(int)TileType.Water] = new Sprite[16];
            tiles = def.SelectNodes("water/t");
            foreach (XmlNode t in tiles)
            {
                int slopeIndex = int.Parse(t.Attributes["slope"].Value);
                int x = int.Parse(t.Attributes["x"].Value);
                int y = int.Parse(t.Attributes["y"].Value);
                int w = int.Parse(t.Attributes["w"].Value);
                int h = int.Parse(t.Attributes["h"].Value);               
                sprites[(int)TileType.Water][slopeIndex] = new Sprite(tex, new Rectangle(x, y, w, h), Color.White);
            }

            int c = 0;
            XmlNodeList houseLevels = def.SelectNodes("houses/district");
            houseBuildings = new Sprite[5][];
            for(int i = 0; i < 4; i++)
            {
                c = 0;
                DistrictType type = (DistrictType)Enum.Parse(typeof(DistrictType), houseLevels[i].Attributes["type"].Value);
                tiles = houseLevels[i].SelectNodes("t");
                houseBuildings[(int)type] = new Sprite[tiles.Count];
                foreach (XmlNode t in tiles)
                {                
                    int x = int.Parse(t.Attributes["x"].Value);
                    int y = int.Parse(t.Attributes["y"].Value);
                    int w = int.Parse(t.Attributes["w"].Value);
                    int h = int.Parse(t.Attributes["h"].Value);
                    houseBuildings[(int)type][c++] = new Sprite(tex, new Rectangle(x, y, w, h), Color.White);
                }
            }

            tiles = def.SelectNodes("pizzaBuildings/t");
            sprites[(int)TileType.Pizza] = new Sprite[tiles.Count];
            c = 0;
            foreach(XmlNode t in tiles)
            {
                int x = int.Parse(t.Attributes["x"].Value);
                int y = int.Parse(t.Attributes["y"].Value);
                int w = int.Parse(t.Attributes["w"].Value);
                int h = int.Parse(t.Attributes["h"].Value);
                sprites[(int)TileType.Pizza][c++] = new Sprite(tex, new Rectangle(x, y, w, h), Color.White);
            }


            tiles = def.SelectNodes("forest/t");
            sprites[(int)TileType.Forest] = new Sprite[tiles.Count];
            c = 0;
            foreach (XmlNode t in tiles)
            {
                int x = int.Parse(t.Attributes["x"].Value);
                int y = int.Parse(t.Attributes["y"].Value);
                int w = int.Parse(t.Attributes["w"].Value);
                int h = int.Parse(t.Attributes["h"].Value);
                sprites[(int)TileType.Forest][c++] = new Sprite(tex, new Rectangle(x, y, w, h), Color.White);
            }

            tiles = def.SelectNodes("stone/t");
            sprites[(int)TileType.Stone] = new Sprite[tiles.Count];
            c = 0;
            foreach (XmlNode t in tiles)
            {
                int x = int.Parse(t.Attributes["x"].Value);
                int y = int.Parse(t.Attributes["y"].Value);
                int w = int.Parse(t.Attributes["w"].Value);
                int h = int.Parse(t.Attributes["h"].Value);
                sprites[(int)TileType.Stone][c++] = new Sprite(tex, new Rectangle(x, y, w, h), Color.White);
            }

            tiles = def.SelectNodes("coal/t");
            sprites[(int)TileType.Coal] = new Sprite[tiles.Count];
            c = 0;
            foreach (XmlNode t in tiles)
            {
                int x = int.Parse(t.Attributes["x"].Value);
                int y = int.Parse(t.Attributes["y"].Value);
                int w = int.Parse(t.Attributes["w"].Value);
                int h = int.Parse(t.Attributes["h"].Value);
                sprites[(int)TileType.Coal][c++] = new Sprite(tex, new Rectangle(x, y, w, h), Color.White);
            }

            tiles = def.SelectNodes("ore/t");
            sprites[(int)TileType.Ore] = new Sprite[tiles.Count];
            c = 0;
            foreach (XmlNode t in tiles)
            {
                int x = int.Parse(t.Attributes["x"].Value);
                int y = int.Parse(t.Attributes["y"].Value);
                int w = int.Parse(t.Attributes["w"].Value);
                int h = int.Parse(t.Attributes["h"].Value);
                sprites[(int)TileType.Ore][c++] = new Sprite(tex, new Rectangle(x, y, w, h), Color.White);
            }

            tiles = def.SelectNodes("oil/t");
            sprites[(int)TileType.Oil] = new Sprite[tiles.Count];
            c = 0;
            foreach (XmlNode t in tiles)
            {
                int x = int.Parse(t.Attributes["x"].Value);
                int y = int.Parse(t.Attributes["y"].Value);
                int w = int.Parse(t.Attributes["w"].Value);
                int h = int.Parse(t.Attributes["h"].Value);
                sprites[(int)TileType.Oil][c++] = new Sprite(tex, new Rectangle(x, y, w, h), Color.White);
            }

            tiles = def.SelectNodes("roads/t");
            roadSprites = new Dictionary<Tuple<int, int>, Sprite>(tiles.Count);
            c = 0;
            foreach (XmlNode t in tiles)
            {
                int roadDir = int.Parse(t.Attributes["roadDir"].Value);
                int slope   = int.Parse(t.Attributes["slope"].Value);
                int x = int.Parse(t.Attributes["x"].Value);
                int y = int.Parse(t.Attributes["y"].Value);
                int w = int.Parse(t.Attributes["w"].Value);
                int h = int.Parse(t.Attributes["h"].Value);

                roadSprites.Add(new Tuple<int, int>(slope, roadDir), new Sprite(tex, new Rectangle(x, y, w, h), Color.White));                    
            }

            tiles = def.SelectNodes("bridges/t");
            bridgeSprites = new Dictionary<Tuple<int, int>, Sprite>(tiles.Count);            
            c = 0;
            foreach(XmlNode t in tiles)
            {
                int roadDir = int.Parse(t.Attributes["roadDir"].Value);
                int slope = int.Parse(t.Attributes["slope"].Value);
                int x = int.Parse(t.Attributes["x"].Value);
                int y = int.Parse(t.Attributes["y"].Value);
                int w = int.Parse(t.Attributes["w"].Value);
                int h = int.Parse(t.Attributes["h"].Value);

                bridgeSprites.Add(new Tuple<int, int>(slope, roadDir), new Sprite(tex, new Rectangle(x, y, w, h), Color.White));
            }

            tiles = def.SelectNodes("highlightSprites/s");
            hightlightSprites = new Dictionary<string, Sprite>();
            foreach(XmlNode s in tiles)
            {
                string name = s.Attributes["name"].Value;
                int x = int.Parse(s.Attributes["x"].Value);
                int y = int.Parse(s.Attributes["y"].Value);
                int w = int.Parse(s.Attributes["w"].Value);
                int h = int.Parse(s.Attributes["h"].Value);

                Rectangle rect = new Rectangle(x, y, w, h);

                Animation anim = new Animation(6, new double[] { 112, 96, 80, 64, 80, 96 },
                                                        new[] { rect },
                                                        new[] { new Point(0, 0), new Point(0, 2), new Point(0, 4), new Point(0, 6), new Point(0,4), new Point(0, 2) });

                hightlightSprites.Add(name, new Sprite(tex, rect, Color.White, anim));

            }
        }        

        public Sprite GetLandscapeSprite(TileType type, int slopeOrIndex)
        {
            Debug.Assert(type == TileType.Nothing || type == TileType.Water);
            if (type == TileType.Water)
                return sprites[(int)TileType.Water][slopeOrIndex];
            else
                return sprites[(int)TileType.Nothing][slopeOrIndex];            
        }

        public Sprite GetOnTopSprite(TileType type, int index)
        {
            Debug.Assert(type != TileType.Nothing && type != TileType.Water && type != TileType.House);
            return sprites[(int)type][index];
        }

        public Sprite GetSprite(TileType type, int index)
        {            
            return sprites[(int)type][index];
        }

        public Sprite GetHouseSprite(DistrictType level, int index)
        {
            return houseBuildings[(int)level][index];
        }

        public Sprite GetRoadSprite(int roadDir, int slope)
        {
            Debug.Assert(roadDir >= 0 && roadDir < 20 && (slope == 0 || slope == 3 || slope == 6 || slope == 9 || slope == 12));
            return roadSprites[new Tuple<int, int>(slope, roadDir)];
        }

        public Sprite GetBridgeSprite(int roadDir, int slope)
        {
            var t = new Tuple<int, int>(slope, roadDir);
            if (bridgeSprites.ContainsKey(t))
                return bridgeSprites[t];
            else
                return roadSprites[new Tuple<int, int>(0, 0)];
        }

        public int GetRandomHouseIndex(DistrictType district)
        {
            Debug.Assert(district != DistrictType.None);
            Sprite[] array = houseBuildings[(int)district];
            return random.Next(array.Length);
        }
        
        public int GetRandomHouseIndex(DistrictType district, Random r)
        {
            Debug.Assert(district != DistrictType.None);
            Sprite[] array = houseBuildings[(int)district];
            return r.Next(array.Length);
        }

        public int GetRandomIndex(TileType type)
        {
            Debug.Assert(type != TileType.Nothing && type != TileType.Road && type != TileType.Water && type != TileType.House);
            return random.Next(sprites[(int)type].Length);
        }

        public int GetRandomIndex(TileType type, Random r)
        {
            Debug.Assert(type != TileType.Nothing && type != TileType.Road && type != TileType.Water && type != TileType.House);
            return r.Next(sprites[(int)type].Length);
        }


        public Sprite GetHightlightSprite(string spriteId)
        {
            return hightlightSprites[spriteId];
        }

    }
}
