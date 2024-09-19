using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Grasshopper.Kernel.Geometry.SpatialTrees;

namespace Moth
{
    public class SortByVector : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SortByVector class.
        /// </summary>
        public SortByVector()
          : base("SortByVector", "SVector",
              "Sort points along vector",
              "Moth", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("List of Points", "P", "List of points", GH_ParamAccess.list);
            pManager.AddVectorParameter("Vector", "V", "Vector to sort points along", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Sorted", "S", "List of sorted points by distance", GH_ParamAccess.list);
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

            Vector3d V = new Vector3d(0, 0, 0);
            DA.GetData(1, ref V);

            //Functions
            //ScalarProjection
            double ScalarProjection(Vector3d A, Vector3d B)
            {
                double DotProduct = A.X * B.X + A.Y * B.Y + A.Z * B.Z;
                double ScalarP = DotProduct / B.Length;

                return ScalarP;
            }

            //Algorithm
            //Unitize reference vector
            V.Unitize();
            //Scalar Projection of the points in P to vector V
            List<double> Projected = new List<double>();
            foreach (Point3d point in P)
            {
                //Convert Point in vector
                Vector3d VectorP = new Vector3d(point.X, point.Y, point.Z);
                //Scalar Projection
                double Scalar = ScalarProjection(VectorP, V);
                Projected.Add(Scalar);
            }

            // Create a list of tuples where each tuple contains the original index, curve and length
            List<Tuple<int, Point3d, double>> indexedC = new List<Tuple<int, Point3d, double>>();
            int count = P.Count;
            for (int n = 0; n < count; n++)
            {

                indexedC.Add(Tuple.Create(n, P[n], Projected[n]));

            }

            // Sort the indexedC list based on the lengths
            indexedC.Sort((a, b) => a.Item3.CompareTo(b.Item3));

            Point3d[] PointsSorted = new Point3d[count];
            int[] index = new int[count];

            for (int m = 0; m < count; m++)
            {
                PointsSorted[m] = indexedC[m].Item2; // Accessing the Curve from the tuple
                index[m] = indexedC[m].Item1; // Accessing the index from the tuple
            }

            //Outputs
            DA.SetDataList(0, PointsSorted);
            DA.SetDataList(1, index);
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
                var ImageBytes = Moth.Properties.Resources.SortVector;
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
            get { return new Guid("D163013C-DBE1-4649-8C43-53C71A3666E6"); }
        }
    }
}