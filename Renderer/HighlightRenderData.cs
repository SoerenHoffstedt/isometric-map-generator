using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.Renderer
{
    public struct HighlightTileRenderData
    {
        public Point coordinate;
        public string spriteId;
        public Color color;
        public int yOffset;

        public HighlightTileRenderData(Point coordinate, string spriteId, Color color, int yOffset)
        {
            this.coordinate = coordinate;
            this.spriteId   = spriteId;
            this.color      = color;
            this.yOffset    = yOffset;
        }
    }
}
