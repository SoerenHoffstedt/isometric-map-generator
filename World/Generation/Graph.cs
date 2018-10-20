using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Barely.Util.Priority_Queue;

namespace Industry.World.Generation
{
    public class RoomGraph
    {        
        public List<Room> rooms;
        public Dictionary<Room, List<Room>> connections;

        public RoomGraph()
        {
            rooms       = new List<Room>(32);
            connections = new Dictionary<Room, List<Room>>(32);
        }

        public void AddRoomsAndConnectAll(List<Room> toAdd)
        {
            for (int i = 0; i < toAdd.Count; i++)
            {                
                rooms.Add(toAdd[i]);
                connections.Add(toAdd[i], new List<Room>(8));
            }

            for(int i = 0; i < rooms.Count; i++)
            {
                Room r = rooms[i];
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    Room r2 = rooms[j];
                    connections[r].Add(r2);
                    connections[r2].Add(r);
                }
            }
        }

        public void AddConnection(Room from, Room to)
        {
            if (!rooms.Contains(from))
            {
                rooms.Add(from);
                connections.Add(from, new List<Room>(8));
            }
            connections[from].Add(to);
        }

        public void AddConnectionBothWays(Room from, Room to)
        {
            if (!rooms.Contains(from))
            {
                rooms.Add(from);
                connections.Add(from, new List<Room>(8));
            }
            if (!rooms.Contains(to))
            {
                rooms.Add(to);
                connections.Add(to, new List<Room>(8));
            }
            connections[from].Add(to);
            connections[to].Add(from);
        }

        public int Count
        {
            get { return rooms.Count; }
        }

        /// <summary>
        /// Creates a minimum spanning tree using Prim's algorithm. The graph this is called on has to be connected.
        /// </summary>
        /// <param name="random">Random object for getting random starter element. As parameter to keep random seed predictability.</param>
        /// <returns>The minimum spanning tree.</returns>
        public RoomGraph MinSpanningTree(Random random, Func<Room, Room, double> DistFunc)
        {
            if (Count == 0)
                return new RoomGraph();

            SimplePriorityQueue<Room> queue = new SimplePriorityQueue<Room>();
            Dictionary<Room, double> distanceToSpannTree = new Dictionary<Room, double>(Count);
            Dictionary<Room, Room> parentRoom = new Dictionary<Room, Room>(Count);       

            foreach(Room r in rooms)
            {
                queue.Enqueue(r, double.MaxValue);
                distanceToSpannTree.Add(r, double.MaxValue);
                parentRoom.Add(r, null);
            }

            int start = random.Next(Count);
            distanceToSpannTree[rooms[start]] = 0;
            queue.UpdatePriority(rooms[start], 0);

            while(queue.Count > 0)
            {
                Room r = queue.Dequeue();
                foreach(Room other in connections[r])
                {
                    if (queue.Contains(other))  //could keep track of this separatly (HashSet) because this is linear. 
                    {
                        double dist = DistFunc(r, other);
                        if(dist < distanceToSpannTree[other])
                        {
                            parentRoom[other] = r;
                            distanceToSpannTree[other] = dist;
                            queue.UpdatePriority(other, dist);
                        }
                    }                    
                }
            }

            //insert the parentNode pairs into a new graph
            RoomGraph minSpanTree = new RoomGraph();            
            foreach(Room r in rooms)
            {                
                minSpanTree.rooms.Add(r);
                minSpanTree.connections.Add(r, new List<Room>(8));
            }            

            foreach (var p in parentRoom)
            {                                 
                if(p.Key != null && p.Value != null)
                {
                    minSpanTree.AddConnectionBothWays(p.Key, p.Value);                 
                }
            }

            return minSpanTree;
        }

        /// <summary>
        /// Puts all connections between rooms in a list of pairs of rooms.
        /// </summary>
        /// <returns>A list of pairs of connected rooms.</returns>
        public List<(Room,Room)> ToConnectionList()
        {            
            HashSet<(Room, Room)> cons = new HashSet<(Room, Room)>();            

            foreach(Room r in rooms)
            {
                foreach(Room neighbour in connections[r])
                {
                    var pair1 = (r, neighbour);
                    var pair2 = (neighbour, r);
                    if (!cons.Contains(pair1) && !cons.Contains(pair2))
                        cons.Add(pair1);
                }
            }

            List<(Room, Room)> toReturn = new List<(Room, Room)>(cons.Count);

            foreach(var pair in cons)
            {
                toReturn.Add(pair);
            }

            return toReturn;
        }

    }

}
