using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System.Linq;
using System.IO;
using Grasshopper.Kernel.Data;
using Grasshopper;

namespace Moth
{
    public class GraphFromLinesComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GraphFromLinesComponent class.
        /// </summary>
        public GraphFromLinesComponent()
          : base("Graph From Lines", "GraphLines",
              "Creates a graph data structure from lines",
              "Moth", "Graph")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Lines", "L", "List of lines", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Graph", "G", "Graph of indices", GH_ParamAccess.tree);
            pManager.AddPointParameter("Points", "P", "Points of graph", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            //Input
            List<Curve> curves = new List<Curve>();
            DA.GetDataList(0, curves);

            List<Line> L = new List<Line>();
            // Iterate through each curve and check if itS a line
            foreach (var curve in curves)
            {
                if (curve.IsLinear()) // Check if the curve is linear
                {
                    // Create a line from the curve's start and end points
                    Line line = new Line(curve.PointAtStart, curve.PointAtEnd);
                    L.Add(line); // Add to the list of lines
                }
                else
                {
                    // Handle non-linear curves if needed
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more curves are not linear.");
                }
            }


            //Functions
            //FindClosestPoint
            Point3d FindClosestPt(Point3d Pt, Dictionary<Point3d, int> pointIndex)
            {
                double closestDistance = double.MaxValue; // Start with a very large number
                Point3d closestPoint = Point3d.Unset;     // Initialize with a default value

                foreach (var entry in pointIndex)
                {
                    Point3d currentPoint = entry.Key;
                    double distance = Pt.DistanceTo(currentPoint);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPoint = currentPoint;
                    }
                }

                return closestPoint;

            }


            // Returns truu if coordinates of points are similar under a threshold value
            bool AreSimilar(Point3d a, Point3d b)
            {
                bool similar = Math.Abs(a.X - b.X) < 0.001
                  && Math.Abs(a.Y - b.Y) < 0.001
                  && Math.Abs(a.Z - b.Z) < 0.001;

                return similar;
            }

            //Algorithm
            // Initiate empty list for points
            List<Point3d> points = new List<Point3d>();

            // Add start and end points of each line
            foreach (Line ln in L)
            {
                if (!points.Contains(ln.From)) points.Add(ln.From);
                if (!points.Contains(ln.To)) points.Add(ln.To);
            }

            // Clean up the list of duplicates
            List<Point3d> ToRemove = new List<Point3d>();
            for (int i = 0; i < points.Count; i++)
            {
                Point3d Pt = points[i];

                for (int j = points.Count - 1; j > i; j--)
                {
                    Point3d other = points[j];

                    bool dup = AreSimilar(Pt, other);
                    if (dup == true)
                    {
                        ToRemove.Add(points[j]);
                    }
                }
            }

            // Convert the list to a set to remove duplicates
            HashSet<Point3d> uniquePointsSet = new HashSet<Point3d>(points);
            // Remove points that are in the toRemove set
            uniquePointsSet.ExceptWith(ToRemove);

            // Convert the set back to a list to get a list of unique points
            List<Point3d> uniquePointsList = new List<Point3d>(uniquePointsSet);

            // Initiate empty dictionary for graph
            Dictionary<int, List<int>> graph = new Dictionary<int, List<int>>();

            // Create a dictionary to map each unique point to an index
            Dictionary<Point3d, int> pointToIndex = new Dictionary<Point3d, int>();

            // Assign an index to each unique point
            for (int i = 0; i < uniquePointsList.Count; i++)
            {
                pointToIndex[uniquePointsList[i]] = i;
                graph[i] = new List<int>();
            }


            // Iterate through each line and add connections
            foreach (Line ln in L)
            {
                Point3d From = FindClosestPt(ln.From, pointToIndex);
                int fromIndex = pointToIndex[From];

                Point3d To = FindClosestPt(ln.To, pointToIndex);
                int toIndex = pointToIndex[To];

                // Add the connections to each index
                if (!graph[fromIndex].Contains(toIndex)) graph[fromIndex].Add(toIndex);
                if (!graph[toIndex].Contains(fromIndex)) graph[toIndex].Add(fromIndex);
            }

            // Make a tree from the dictionary
            DataTree<int> graphTree = new DataTree<int>();
            for (int i = 0; i < graph.Count; i++)
            {
                for (int j = 0; j < graph[i].Count; j++)
                {

                    graphTree.Add(graph[i][j], new GH_Path(i));
                }
            }

            // Output
            DA.SetDataTree(0, graphTree);
            DA.SetDataList(1, uniquePointsList);

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
                var ImageBytes = Moth.Properties.Resources.GraphFromLines;
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
            get { return new Guid("E8B46A1B-D8A0-4CCC-8E94-9CAE24C4C0F8"); }
        }
    }
}