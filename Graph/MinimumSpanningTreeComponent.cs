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
    public class MinimumSpanningTreeComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MinimunSpanningTreeComponent class.
        /// </summary>
        public MinimumSpanningTreeComponent()
          : base("Minimun Spanning Tree", "MST",
              "Minimun spanning tree of a graph",
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
            pManager.AddCurveParameter("MTS", "T", "Minimun Spanning Tree", GH_ParamAccess.list);
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
            
            List <Point3d> P = new List<Point3d>();
            DA.GetDataList(1, P);

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
            // Ffind the root parent
            int Find(int[] parents, int i)
            {
                if (parents[i] != i)
                {
                    parents[i] = Find(parents, parents[i]); // Path compression
                }
                return parents[i];
            }


            //Apply Union
            void ApplyUnion(int[] parents, int[] rank, int x, int y)
            {
                int rootX = Find(parents, x);
                int rootY = Find(parents, y);

                if (rootX != rootY)
                {
                    // Attach the tree with lower rank to the tree with higher rank
                    if (rank[rootX] > rank[rootY])
                    {
                        parents[rootY] = rootX;
                    }
                    else if (rank[rootX] < rank[rootY])
                    {
                        parents[rootX] = rootY;
                    }
                    else
                    {
                        // If ranks are equal, choose one as the new root and increase its rank
                        parents[rootY] = rootX;
                        rank[rootX]++;
                    }
                }
            }

            // Get list of edges
            int[][] GetEdges(DataTree<int> tree)
            {
                HashSet<KeyValuePair<int, int>> hashEdges = new HashSet<KeyValuePair<int, int>>();

                for (int i = 0; i < tree.BranchCount; i++)
                {
                    foreach (int j in tree.Branch(i))
                    {
                        KeyValuePair<int, int> edge1 = new KeyValuePair<int, int>(i, j);
                        KeyValuePair<int, int> edge2 = new KeyValuePair<int, int>(j, i);

                        if (!hashEdges.Contains(edge2))
                        {
                            hashEdges.Add(edge1);
                        }
                    }
                }

                // Convert HashSet to array int[][]
                int[][] edges = hashEdges.Select(edge => new int[] { edge.Key, edge.Value }).ToArray();
                return edges;
            }

            // Sort edges by length
            void Sort(int[][] edges, List<Point3d> points)
            {
                Array.Sort(edges, (edge1, edge2) =>
                {
                    double length1 = new Line(points[edge1[0]], points[edge1[1]]).Length;
                    double length2 = new Line(points[edge2[0]], points[edge2[1]]).Length;
                    return length1.CompareTo(length2);
                });
            }

            //Algorithm
            //Create set of edges
            int[][] Edges = GetEdges(G);
            // Sort edges
            Sort(Edges, P);

            //Set parents and rank
            int[] Parents = Enumerable.Range(0, P.Count).ToArray();
            int[] Rank = new int[P.Count];

            //edge counter
            int edgeCount = 0;

            //Empty list of edges
            List<int[]> mstEdges = new List<int[]>();

            for (int i = 0; i < Edges.Length && edgeCount < P.Count - 1; i++)
            {
                int[] edge = Edges[i];
                int u = edge[0];
                int v = edge[1];

                int rootU = Find(Parents, u);
                int rootV = Find(Parents, v);

                if (rootU != rootV)
                {
                    mstEdges.Add(edge);
                    edgeCount++;
                    ApplyUnion(Parents, Rank, rootU, rootV);
                }
            }

            //Draw Lines
            List<Line> Lines = new List<Line>();

            foreach (int[] edge in mstEdges)
            {
                Line line = new Line(P[edge[0]], P[edge[1]]);
                Lines.Add(line);
            }
            //Output

            DA.SetDataList(0, Lines);
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
                var ImageBytes = Moth.Properties.Resources.MinimumSpanningTree;
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
            get { return new Guid("676B8330-6FA6-4FC1-B677-50B4C6780AE0"); }
        }
    }
}