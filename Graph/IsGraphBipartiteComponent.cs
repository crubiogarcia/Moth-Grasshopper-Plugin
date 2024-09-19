using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Linq;

namespace Moth
{
    public class IsGraphBipartiteComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the IsGraphBipartiteComponent class.
        /// </summary>
        public IsGraphBipartiteComponent()
          : base("Is Graph Bipartite", "GBipartite",
              "Check if graph is bipartite and if it is, it retunr the two sets of nodes",
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
            pManager.AddBooleanParameter("Result", "R", "Returns True if graph G is bipartite", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Indices sets", "S", "Sets of indices if graph G is bipartite", GH_ParamAccess.tree);
            pManager.AddPointParameter("Set A", "A", "Set A of points if graph G is bipartite", GH_ParamAccess.list);
            pManager.AddPointParameter("Set B", "B", "Set B of points if graph G is bipartite", GH_ParamAccess.list);
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

            //Algorithm
            // Get List of nodes
            List<int> Nodes = Enumerable.Range(0, G.BranchCount).ToList();

            // Create a List for queue
            Queue<int> Qu = new Queue<int>();

            // Initialize List for 'colors'
            List<int> Colors = Enumerable.Repeat(-1, Nodes.Count).ToList();

            bool Result = true;

            DataTree<int> Sets = new DataTree<int>();
            List<Point3d> a = new List<Point3d>();
            List<Point3d> b = new List<Point3d>();

            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Colors[i] == -1)
                {
                    Qu.Enqueue(i);
                    Colors[i] = 0;
                    a.Add(P[i]);  // Add the starting node to list 'a'

                    while (Qu.Count != 0)
                    {
                        int node = Qu.Dequeue();

                        foreach (int neig in G.Branch(node))
                        {
                            if (Colors[neig] == -1)
                            {
                                Colors[neig] = 1 - Colors[node];

                                // Add the neighbor to the correct list based on its color
                                if (Colors[neig] == 0)
                                {
                                    a.Add(P[neig]);
                                    GH_Path path = new GH_Path(0);
                                    Sets.Add(neig, path);
                                }
                                else
                                {
                                    b.Add(P[neig]);
                                    GH_Path path = new GH_Path(1);
                                    Sets.Add(neig, path);
                                }

                                Qu.Enqueue(neig);
                            }
                            else if (Colors[neig] == Colors[node])
                            {
                                Result = false;
                                // Optionally, you can break out of the loop here since the graph is not bipartite.
                                break;
                            }
                        }

                        // Optionally, if Result is false, you can break here to stop further processing
                        if (!Result)
                        {
                            break;
                        }
                    }
                }

                // Optionally, if Result is false, you can break here to stop further processing
                if (!Result)
                {
                    break;
                }
            }

            // Return results
            DA.SetData(0, Result);
            DA.SetDataTree(1, Sets);
            DA.SetDataList(2, a);
            DA.SetDataList(3, b);

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
                var ImageBytes = Moth.Properties.Resources.IsGraphBipartite;
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
            get { return new Guid("A2C6FD0B-0C43-4E8A-8B3F-2399C14DE24A"); }
        }
    }
}