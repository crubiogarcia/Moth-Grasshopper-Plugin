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
    public class BetweenessCentralityComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ClosenessCentralityComponent class.
        /// </summary>
        public BetweenessCentralityComponent()
          : base("Betweeness Centrality", "BCentrality",
              "Betweeness Centrality of the nodes of a graph",
              "Moth", "Graph")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Graph", "G", "Graph tree", GH_ParamAccess.tree);
            pManager.AddPointParameter("List of Points", "P", "List of points", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Centrality", "C", "Betweeness Centrality", GH_ParamAccess.list);
            pManager.AddNumberParameter("Normalized Centrality", "N", "Normalized Betweeness Centrality", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Input
            GH_Structure<GH_Integer> G_in = new GH_Structure<GH_Integer>();
            if (!DA.GetDataTree(0, out G_in)) return;

            List<Point3d> P = new List<Point3d>();
            if (!DA.GetDataList(1, P)) return;

            // Create a new DataTree<int> to store the converted values
            DataTree<int> G = new DataTree<int>();
            // Iterate over all paths and convert the GH_Integer values to int
            foreach (GH_Path path in G_in.Paths)
            {
                List<int> branch = new List<int>();

                // Get the list of GH_Integer values in the current branch
                List<GH_Integer> ghIntegers = G_in.get_Branch(path).Cast<GH_Integer>().ToList();

                // Convert GH_Integer to int and add to the new branch
                foreach (GH_Integer ghInt in ghIntegers)
                {
                    branch.Add(ghInt.Value); // Extract the int value from GH_Integer
                }

                // Add the converted branch to the DataTree<int> with the same path
                G.AddRange(branch, path);
            }

            //Functions
            //Shortest Path
            List<int> ShortestPath(DataTree<int> Gr, int Start, int End)
            {
                //Create a List for Visited
                List<Boolean> Visited = new List<Boolean>();
                for (int i = 0; i < Gr.BranchCount; i++)
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
                    List<int> neighbours = Gr.Branch(node);

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

                return Path;
            }

            //Get Queue
            List<int> GetPath(int Start, int End, Dictionary<int, int> Prev)
            {
                //New List fot the path
                List<int> Path = new List<int>();
                //Add each step in the path
                for (int item = End; item != Start; item = Prev[item])
                {
                    Path.Add(item);
                }
                //Add Starting node
                Path.Add(Start);

                //Reverse List
                Path.Reverse();

                //return path
                return Path;
            }

            //Algorithm

            //Get List of nodes
            List<int> Nodes = Enumerable.Range(0, G.BranchCount).ToList();

            //Initiate empty list of centrality counter
            int[] centralityArray = new int[Nodes.Count];
            List<int> Counter = centralityArray.ToList();
            //Initiate empty list of centrality
            List<double> Centrality = new List<double>();


            //Iterate through each node
            foreach (int Node in Nodes)
            {
                foreach (int End in Nodes)
                {
                    if (Node != End)
                    {
                        //Get shortest path
                        List<int> Spath = ShortestPath(G, Node, End);

                        for (int i = 1; i < Spath.Count - 1; i++)
                        {
                            int Visited = Spath[i];
                            Counter[Visited] += 1;
                        }

                    }
                }

            }

            //Normalize Centrality
            double min = Counter.Min();
            double max = Counter.Max();

            List<double> Normalized = new List<double>();
            foreach (int num in Counter)
            {
                Normalized.Add((num - min) / (max - min));
            }

            //Output
            DA.SetDataList(0, Counter);
            DA.SetDataList(1, Normalized);

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
                var ImageBytes = Moth.Properties.Resources.BetweenessCentrality;
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
            get { return new Guid("DD4F65F0-757A-4701-A240-E21BF4C8E604"); }
        }
    }
}