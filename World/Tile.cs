using Barely.Util;
using Industry.Simulation;
using Industry.World.Generation;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.World
{
    public class Tile
    {
        public Point coord;
        public int[] height;
        public TileType type;
        public int onTopIndex;        
        public Color color = Color.White;

        public Room partOfRoom = null;

        public Store store;
        public City city;
        public CitizenLevel citizenLevel { get; private set; } = CitizenLevel.None;


        public Tile(Point coord, int n, int e, int s, int w)
        {
            this.coord = coord;
            height = new int[] { n, e, s, w };            
            type = TileType.Nothing;            
        }

        public void SetCitizenLevel(CitizenLevel level)
        {
            this.citizenLevel = level;
        }

        public void SetHeights(int n, int e, int s, int w)
        {
            height[0] = n;
            height[1] = e;
            height[2] = s;
            height[3] = w;
        }

        public void SetAllHeights(int h)
        {
            for(int i = 0; i < height.Length; i++)
            {
                height[i] = h;
            }
        }

        public int GetMaxHeight()
        {
            int m = height[0];
            for(int i = 1; i < 4; i++)
            {
                if (m < height[i])
                    m = height[i];
            }
            return m;                
        }

        public int GetSlopeIndex()
        {
            int m = GetMaxHeight();            
            int slope = 0;
            for (int i = 0; i < 4; i++)
            {
                if (height[i] < m)
                {
                    switch (i)
                    {
                        case 0:
                            slope += (int)SlopeIndex.North;
                            break;
                        case 1:
                            slope += (int)SlopeIndex.East;
                            break;
                        case 2:
                            slope += (int)SlopeIndex.South;
                            break;
                        case 3:
                            slope += (int)SlopeIndex.West;
                            break;
                    }                    
                }
            }
            return slope;
        }

        public bool AllHeightsAreSame()
        {            
            for (int i = 1; i < height.Length; i++)
            {
                if (height[i] != height[0])
                    return false;
            }
            return true;
        }

        public bool IsHousePlaceable()
        {
            return AllHeightsAreSame() && type == TileType.Nothing;
        }

        public bool IsRoadPlaceable(bool isWaterPlaceable = true)
        {
            if (type == TileType.Water && !isWaterPlaceable)
                return false;

            if (AllHeightsAreSame())
                return true;
            else
            {
                // a ramp slope
                int slope = GetSlopeIndex();
                return slope == 3 || slope == 6 || slope == 12 || slope == 9;
            }
        }

    }

    public enum SlopeIndex
    {        
        North   = 1,
        East    = 2,
        South   = 4,
        West    = 8  
    }

    public enum TileType
    {
        Nothing,
        Water,
        House,
        Forest,
        Road,
        Bridge,
        Pizza
    }
}
