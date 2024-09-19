using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Rhino.DocObjects;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Moth
{
    public class ShortestPathComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ShortestPathComponent class.
        /// </summary>
        public ShortestPathComponent()
          : base("Shortest Path", "SPath",
              "Shortest Path between two points in a graph",
              "Moth", "Graph")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Graph", "G", "Graph tree", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Index of starting point", "P0", "Index of starting point", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Index of ending point", "P1", "Index of ending point", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Path", "P", "Path of indices from P0 to P1", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Input
            GH_Structure<GH_Integer> G = new GH_Structure<GH_Integer>();
            if (!DA.GetDataTree(0, out G)) return;
            int Start = 0;
            if (!DA.GetData(1, ref Start)) return;
            int End = 0;
            if (!DA.GetData(2, ref End)) return;

            //Function
            //Get Queue
            List<int> GetPath(int S, int E, Dictionary<int, int> Prv)
            {
                //New List fot the path
                List<int> Pth = new List<int>();
                //Add each step in the path
                for (int item = E; item != S; item = Prv[item])
                {
                    Pth.Add(item);
                }
                //Add Starting node
                Pth.Add(S);

                //Reverse List
                Pth.Reverse();

                //return path
                return Pth;

            }

            //Algorithm
            //Create a List for Visited
            List<Boolean> Visited = new List<Boolean>();
            for (int i = 0; i < G.PathCount; i++)
            {
                Visited.Add(false);
            }
            //Set Start Visited to True
            Visited[Start] = true;

            //Create a List for previous
            Dictionary<int, int> Prev = new Dictionary<int, int>();


            //Create a List for queue
            Queue<int> Qu = new Queue<int>();
            //Add Start to Queue
            Qu.Enqueue(Start);

            //Create empty Path
            List<int> Path = new List<int>();

            //Find the shortest path
            while (Qu.Count != 0)
            {
                //Access node
                int node = Qu.Dequeue();

                if (node == End)
                {
                    Path = GetPath(Start, End, Prev);
                    break;
                }

                GH_Path p = new GH_Path(node);
                List<GH_Integer> ghNeighbours = G[p];

                // Convert List<GH_Integer> to List<int>
                List<int> neighbours = ghNeighbours.Select(n => n.Value).ToList();

                //iterate through the neighbours
                foreach (int j in neighbours)
                {
                    if (Visited[j] == false)
                    {
                        Qu.Enqueue(j);
                        Visited[j] = true;
                        Prev[j] = node;
                    }
                }
            }

            //Output
            DA.SetDataList(0,  Path);
        }

        //Set exposure level
        public override GH_Exposure Exposure => base.Exposure;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var ImageBytes = Moth.Properties.Resources.ShortestPath;
                using (MemoryStream ms = new MemoryStream(ImageBytes))
                {
                    System.Drawing.Bitmap image = new System.Drawing.Bitmap(ms);
                    return image;
                }
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("737E9010-6AD2-4FF7-A4E9-A87E201DCD43"); }
        }
    }
}