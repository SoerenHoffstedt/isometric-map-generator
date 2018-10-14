using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.World.Generation
{
    public class Room
    {
        public HashSet<Point> Tiles { private set; get; }
        public int XMin { private set; get; } = int.MaxValue;
        public int XMax { private set; get; } = -1;

        public int YMin { private set; get; } = int.MaxValue;
        public int YMax { private set; get; } = -1;

        private Point middlePoint = new Point(-1, -1);
        public Point MiddlePoint
        {
            private set { middlePoint = value; }
            get
            {
                if (middlePoint == new Point(-1, -1))
                {
                    middlePoint = CalculateMiddlePoint();
                }
                return middlePoint;
            }
        }

        public Room(HashSet<Point> tiles)
        {
            Tiles = tiles;
            foreach (Point p in tiles)
            {
                if (p.X < XMin)
                    XMin = p.X;
                else if (p.X > XMax)
                    XMax = p.X;

                if (p.Y < YMin)
                    YMin = p.Y;
                else if (p.Y > YMax)
                    YMax = p.Y;
            }
        }

        public Room()
        {
            Tiles = new HashSet<Point>();
        }

        public void Add(Point p)
        {
            Tiles.Add(p);

            if (p.X < XMin)
                XMin = p.X;
            else if (p.X > XMax)
                XMax = p.X;

            if (p.Y < YMin)
                YMin = p.Y;
            else if (p.Y > YMax)
                YMax = p.Y;
        }

        private Point CalculateMiddlePoint()
        {
            Point toReturn = new Point(-1, -1);
            Vector2 diff = new Vector2(-1, -1);

            Vector2 mid = new Vector2((XMax - XMin) / 2 + XMin, (YMax - YMin) / 2 + YMin);
            float toReturnDistance = float.MaxValue;

            foreach (Point p in Tiles)
            {
                if (toReturn == new Point(-1, -1))
                {
                    toReturn = p;
                    toReturnDistance = (mid - p.ToVector2()).Length();
                }
                else
                {
                    float dist = (mid - p.ToVector2()).Length();
                    if (dist < toReturnDistance)
                    {
                        toReturn = p;
                        toReturnDistance = dist;
                    }
                }
            }

            return toReturn;
        }

        public double DistanceToSquared(Room other)
        {
            return (MiddlePoint.ToVector2() - other.MiddlePoint.ToVector2()).LengthSquared();
        }

    }
}
