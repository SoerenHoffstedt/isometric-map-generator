using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.World.Generation.Modules
{
    public interface IGeneratorModule
    {
        void Apply(GeneratorParameter param, Tile[,] tiles);
    }
}
