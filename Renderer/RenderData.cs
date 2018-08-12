using Barely.Util;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.Renderer
{
    public class RenderData
    {
        public Sprite sprite;
        public Point pos;
        public float depth;

        public RenderData(Sprite sprite, Point pos, float depth)
        {
            this.sprite = sprite;
            this.pos = pos;
            this.depth = depth;
        }

    }
}
