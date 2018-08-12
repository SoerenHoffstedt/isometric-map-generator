using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.Simulation
{
    public class PizzaOrder
    {
        public Point    deliverTo;
        public int      price;
        public double   createdOnSimTime;

        public PizzaOrder(Point deliverTo, int price, double createdOnSimTime)
        {
            this.deliverTo          = deliverTo;
            this.price              = price;
            this.createdOnSimTime   = createdOnSimTime;
        }

    }
}
