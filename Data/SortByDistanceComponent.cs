using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;

namespace Moth
{
    public class SortByDistanceComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SortByDistance class.
        /// </summary>
        public SortByDistanceComponent()
          : base("SortByDistance", "SDistance",
              "Sort list of points by distance to point",
              "Moth", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("List of Points", "P", "List of points", GH_ParamAccess.list);
            pManager.AddPointParameter("Parameter", "t", "Point to measure distance from", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Sorted", "S", "List of sorted points by distance", GH_ParamAccess.list);
            pManager.AddNumberParameter("Distances", "D", "Distances to C", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Indices", "i", "List of sorted indices", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Inputs
            List<Point3d> P = new List<Point3d>();
            DA.GetDataList(0, P);
            
            Point3d C = new Point3d(0,0,0);
            DA.GetData(1, ref C);
            
            //Algorithm

            //Start lists empty
            List<Tuple<double, int, Point3d>> DistanceIndex = new List<Tuple<double, int, Point3d>>();

            //Calculate distance of points
            for (int j = 0; j < P.Count; j++)
            {
                double Dist = C.DistanceTo(P[j]);
                DistanceIndex.Add(Tuple.Create(Dist, j, P[j]));
            }

            // Sort the list of tuples by distance
            DistanceIndex.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            // Extract sorted distances, indexes, and points
            List<double> Distances = new List<double>();
            List<int> Indexes = new List<int>();
            List<Point3d> Points = new List<Point3d>();

            foreach (var pair in DistanceIndex)
            {
                Distances.Add(pair.Item1);
                Indexes.Add(pair.Item2);
                Points.Add(pair.Item3);
            }

            //Outputs
            DA.SetDataList(0, Points);
            DA.SetDataList(1, Distances);
            DA.SetDataList(2, Indexes);

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
                var ImageBytes = Moth.Properties.Resources.SortDistance;
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
            get { return new Guid("CE57FDE3-7F93-4903-9D5D-AC003CC50112"); }
        }
    }
}