using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Rhino.DocObjects;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using static GH_IO.VersionNumber;
using GH_IO.Serialization;

namespace Moth
{
    public class SortByLengthComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SortByLengthComponent()
          : base("Sort by Lenth", "SLength",
              "Sort Curves by Length",
              "Moth", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "List of curves", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Sorted", "S", "List of sorted curves by length", GH_ParamAccess.list);
            pManager.AddNumberParameter("Lengths", "L", "List of sorted lengths", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Indices", "i", "List of sorted indices", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Inputs
            List<Curve> C = new List<Curve>();
            DA.GetDataList(0, C);
            double w = 0;
            DA.GetData(0, ref w);
            int s = 0;
            DA.GetData(1, ref s);

            //Algorithm

            // Get the number of elements in C
            int count = C.Count;

            // Create a list of tuples where each tuple contains the original index, curve and length
            List<Tuple<int, Curve, double>> indexedC = new List<Tuple<int, Curve, double>>();

            for (int n = 0; n < count; n++)
            {

                indexedC.Add(Tuple.Create(n, C[n], C[n].GetLength()));

            }

            // Sort the indexedC list based on the lengths
            indexedC.Sort((a, b) => a.Item3.CompareTo(b.Item3));

            Curve[] crvs = new Curve[count];
            double[] lengths = new double[count];
            int[] index = new int[count];

            for (int m = 0; m < count; m++)
            {
                crvs[m] = indexedC[m].Item2; // Accessing the Curve from the tuple
                lengths[m] = indexedC[m].Item3; // Accessing the length from the tuple
                index[m] = indexedC[m].Item1; // Accessing the index from the tuple
            }

            //Output
            DA.SetDataList(0, crvs);
            DA.SetDataList(1, lengths);
            DA.SetDataList(2, index);   



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
                var ImageBytes = Moth.Properties.Resources.SortLengths;
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
            get { return new Guid("B81196E5-5902-4BDD-AC84-AE623FD8BCFE"); }
        }
    }
}