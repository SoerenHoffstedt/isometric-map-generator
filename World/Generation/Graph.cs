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
        public List<Node> nodes;
        public Dictionary<Room, Node> roomToNode;

        public RoomGraph()
        {
            nodes = new List<Node>(32);
            roomToNode = new Dictionary<Room, Node>(32);
        }

        public void AddNodesAndConnectAll(List<Room> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                Node n = new Node(rooms[i]);
                roomToNode.Add(rooms[i], n);
                nodes.Add(n);
            }

            for(int i = 0; i < rooms.Count; i++)
            {
                Node n = roomToNode[rooms[i]];

                for (int j = 0; j < rooms.Count; j++)
                {
                    if (i == j)
                        continue;

                    Node other = roomToNode[rooms[j]];
                    n.AddConnection(other);
                }
            }
        }     

        public double Distance(Node a, Node b)
        {
            return (a.room.MiddlePoint.ToVector2() - b.room.MiddlePoint.ToVector2()).LengthSquared();
        }

        public int Count
        {
            get { return nodes.Count; }
        }

        /// <summary>
        /// Creates a minimum spanning tree using Prim's algorithm. The graph this is called on has to be connected.
        /// </summary>
        /// <param name="random">Random object for getting random starter element. As parameter to keep random seed predictability.</param>
        /// <returns>The minimum spanning tree.</returns>
        public RoomGraph MinSpanningTree(Random random)
        {
            if (nodes.Count == 0)
                return new RoomGraph();

            SimplePriorityQueue<Node> queue = new SimplePriorityQueue<Node>();
            Dictionary<Node, double> distanceToSpannTree = new Dictionary<Node, double>(nodes.Count);
            Dictionary<Node, Node> parentNode = new Dictionary<Node, Node>(nodes.Count);
       
            foreach(Node n in nodes)
            {
                foreach(Node inner in nodes)
                {
                    bool isEqual = n == inner;
                }
            }

            foreach(Node n in nodes)
            {
                queue.Enqueue(n, double.MaxValue);
                distanceToSpannTree.Add(n, double.MaxValue);
                parentNode.Add(n, null);
            }

            int start = random.Next(this.Count);
            distanceToSpannTree[nodes[start]] = 0;
            queue.UpdatePriority(nodes[start], 0);

            while(queue.Count > 0)
            {
                Node n = queue.Dequeue();
                foreach(Node other in n.connections)
                {
                    if (queue.Contains(other))  //could keep track of this separatly (HashSet) because this is linear. 
                    {
                        double dist = Distance(n, other);
                        if(dist < distanceToSpannTree[other])
                        {
                            parentNode[other] = n;
                            distanceToSpannTree[other] = dist;
                            queue.UpdatePriority(other, dist);
                        }
                    }                    
                }
            }

            //insert the parentNode pairs into a new graph
            RoomGraph minSpanTree = new RoomGraph();            
            foreach(Node n in nodes)
            {
                Node newNode = new Node(n.room);
                minSpanTree.nodes.Add(newNode);
                minSpanTree.roomToNode.Add(n.room, newNode);
            }            

            foreach (var p in parentNode)
            {                                 
                if(p.Key != null && p.Value != null)
                {
                    minSpanTree.roomToNode[p.Key.room].AddConnection(minSpanTree.roomToNode[p.Value.room]);                    
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

            foreach(Node n in nodes)
            {
                foreach(Node neighbour in n.connections)
                {
                    cons.Add((n.room, neighbour.room));
                }
            }

            List<(Room, Room)> connections = new List<(Room, Room)>(cons.Count);

            foreach(var pair in cons)
            {
                connections.Add(pair);
            }

            return connections;
        }

    }

    public class Node
    {
        public Room room;       
        public List<Node> connections;

        public Node(Room room){
            this.room = room;
            connections = new List<Node>(16);
        }

        public void AddConnection(Node to)
        {
            connections.Add(to);
        }
        
    }

}
