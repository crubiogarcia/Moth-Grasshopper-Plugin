using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Grasshopper.Kernel.Data;

namespace Moth.Data
{
    public class SortPointsByXComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SortPointsByXComponent()
          : base("Sort Points By X Coordinate", "SPX",
              "Sort points by X Coordinate",
              "Moth", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("List of Points", "P", "List of points", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Sorted", "S", "List of sorted points by X Coordinate", GH_ParamAccess.list);
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

            //Algorithm

            int count = P.Count;


            /// Create a list of tuples where each tuple contains index and point
            List<Tuple<int, Point3d>> indexedP = new List<Tuple<int, Point3d>>();

            for (int n = 0; n < count; n++)
            {
                indexedP.Add(Tuple.Create(n, P[n]));
            }

            // Sort the indexedP list based on the X coordinates of the Point3d
            indexedP.Sort((a, b) => a.Item2.X.CompareTo(b.Item2.X));

            Point3d[] pts = new Point3d[count];
            int[] index = new int[count];

            for (int m = 0; m < count; m++)
            {
                pts[m] = indexedP[m].Item2;
                index[m] = indexedP[m].Item1;
            }

            // Output
            DA.SetDataList(0, pts);
            DA.SetDataList(1, index);
        }

        //Set exposure level
        public override GH_Exposure Exposure => base.Exposure;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        /// 
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var ImageBytes = Moth.Properties.Resources.SortPointsX;
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
            get { return new Guid("C5CFAD0B-5918-438A-AEB4-10589EBBB687"); }
        }
    }
}