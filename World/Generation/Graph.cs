using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.World.Generation
{
    public class Graph
    {
        public List<Node> nodes = new List<Node>(32);

        public void AddNodesAndConnectAll(List<Room> nodes)
        {
            for(int i = 0; i < nodes.Count; i++)
            {
                Node n = new Node(nodes[i]);

                for(int k = 0; k < nodes.Count; k++)
                {
                    if(i != k)
                    {
                        n.AddConnection(nodes[k]);
                    }
                }
            }
        }

        public void CreateMinimalGraph()
        {

        }

        public Graph MinSpanningTree()
        {


            return null;
        }


    }

    public class Node
    {
        public Room room;
        public List<Connection> connections;

        public Node(Room room){
            this.room = room;
            connections = new List<Connection>();
        }

        public void AddConnection(Room to)
        {
            connections.Add(new Connection(to, (room.MiddlePoint - to.MiddlePoint).ToVector2().Length()));
        }
        
    }

    public class Connection
    {
        public Room neighbour;
        public float distance;

        public Connection(Room room, float distance)
        {
            neighbour = room;
            this.distance = distance;
        }
    }



}
