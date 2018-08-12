using Barely.Util;
using Glide;
using Industry.Simulation;
using Industry.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.Agents
{
    public class Agent
    {        
        protected Func<Point, Point> TileToWorld;
        public bool isMoving = false;
        public List<Point> currentPath { get; protected set; }
        protected int pathIndex = 0;
        public Sprite[] sprites;
        public Point tilePosition;
        protected int renderX, renderY;
        protected Direction direction;

        protected double idleTime;
        protected float speed = 0.5f;
        public int baseWage { get; protected set; } = 100;
        protected float happiness;

        public AgentState state { get; protected set; }
        public Store workingFor { get; protected set; }
        public int deliveryCapacity { get; protected set; } = 3;

        private PizzaOrder currentDelivery;
        private Queue<PizzaOrder> deliveries;
        
        int maxDeliveriesTakenAtOnce = 0;

        public Point GetRenderPosition()
        {
            return new Point(renderX, renderY);
        }
        
        public Agent(Sprite[] sprites, Point pos, Func<Point, Point> TileToWorld, Store workingFor)
        {
            this.TileToWorld = TileToWorld;
            this.sprites = sprites;
            tilePosition = pos;
            Point renderPosition = TileToWorld(pos);
            renderX = renderPosition.X;
            renderY = renderPosition.Y;
            state = AgentState.Idle;
            this.workingFor = workingFor;
            deliveries = new Queue<PizzaOrder>();
        }

        public void TakeOrder(PizzaOrder order)
        {
            deliveries.Enqueue(order);
            if (deliveries.Count > maxDeliveriesTakenAtOnce)
                maxDeliveriesTakenAtOnce = deliveries.Count;
            state = AgentState.Delivering;
        }

        public void SetPath(List<Point> newPath, AgentState newState)
        {
            state = newState;
            currentPath = newPath;
            if(currentPath != null && currentPath.Count > 0)
            {
                isMoving = true;
                pathIndex = 0;
                if (currentPath[0] == tilePosition)
                {
                    if (currentPath.Count == 1)
                    {
                        MoveFinished();
                        return;
                    }
                    else
                        pathIndex = 1;
                }
                
                Point t = TileToWorld(currentPath[pathIndex]);
                MovementDirection(tilePosition, currentPath[pathIndex]);
                SetTilepositionBeforeMove();
                Simulation.Simulator.tweener.Tween(this, new { renderX = t.X, renderY = t.Y }, speed).OnComplete(ReachedNextTile);
            }
            else
            {
                if(deliveries.Count == 0 && state == AgentState.Delivering)
                {
                    state = AgentState.DrivingBackToStore;
                }
            }
        }

        public Point GetNextDeliveryTarget()
        {
            currentDelivery = deliveries.Dequeue();
            return currentDelivery.deliverTo;
        }

        public Sprite GetSprite()
        {
            return sprites[(int)direction];
        }

        protected void ReachedNextTile()
        {
            if(pathIndex == currentPath.Count - 1)
            {
                MoveFinished();
            } else
            {
                tilePosition = currentPath[pathIndex];
                pathIndex++;
                Point target = TileToWorld(currentPath[pathIndex]);
                MovementDirection(tilePosition, currentPath[pathIndex]);
                SetTilepositionBeforeMove();
                Simulation.Simulator.tweener.Tween(this, new { renderX = target.X, renderY = target.Y }, speed).OnComplete(ReachedNextTile);
            }
        }

        protected void MoveFinished()
        {
            tilePosition = currentPath[pathIndex];
            currentPath = null;
            isMoving = false;

            if(currentDelivery != null)
            {
                workingFor.DeliveryFinished(currentDelivery);
                currentDelivery = null;
            }

            if(state == AgentState.Delivering && deliveries.Count == 0)
            {
                //Move to store tile is handled by GameScene, that detects if agent has this state and a null path
                state = AgentState.DrivingBackToStore;
            }
            else if(state == AgentState.DrivingBackToStore)
            {
                state = AgentState.Idle;
                workingFor.EmployeeIsBack(this);
            }
        }

        private void MovementDirection(Point from, Point to)
        {
            Point p = from - to;
            Debug.Assert(Math.Abs(p.X + p.Y) == 1);            

            if (to.Y == from.Y - 1)
                direction = Direction.North;
            else if(to.Y == from.Y + 1)
                direction = Direction.South;
            else if (to.X == from.X + 1)
                direction = Direction.East;
            else
                direction = Direction.West;
        }

        private void SetTilepositionBeforeMove()
        {
            switch (direction)
            {
                case Direction.East:
                case Direction.West:             
                    break;
                case Direction.North:
                case Direction.South:
                    tilePosition = currentPath[pathIndex];
                    break;
            }
        }

    }

    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    public enum AgentState
    {
        Delivering,
        DrivingBackToStore,
        Idle,
        Pause
    }
}
