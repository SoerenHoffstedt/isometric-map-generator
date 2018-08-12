using Industry.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.Renderer
{
    public class PlacementPreviewData
    {
        public TileType type;
        public int spriteIndex;

        public PlacementPreviewData(TileType type, int spriteIndex)
        {
            this.type = type;
            this.spriteIndex = spriteIndex;
        }
    }
}
