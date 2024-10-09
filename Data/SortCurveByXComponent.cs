using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Rhino.DocObjects;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.ComponentModel;


namespace Moth
{
    public class SortCurveByXComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SortCurveByXComponent()
          : base("Sort by X", "SortX",
              "Sort by X coordinate",
              "Moth", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "List of curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Parameter", "t", "Normalized parameter", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Sorted", "S", "List of sorted curves by X coordinate", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Indices", "i", "List of sorted indices", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Inputs
            List<Rhino.Geometry.Curve> C = new List<Rhino.Geometry.Curve>();
            DA.GetDataList(0, C);
            double t = 0;
            DA.GetData(1, ref t);

            //Error Handling
            if (t < 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "t must be postive");
                return;
            }
            for (int j = 0; j < C.Count; j++)
            {
                if (t > C[j].GetLength())
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "t must be inside the curve range");
                    return;
                }
            }

            //Algorithm
            int count = C.Count;

            // Get the number of elements in L

            // Create a list of tuples where each tuple contains index, curve and X coordinates
            List<Tuple<int, Rhino.Geometry.Curve, double>> indexedC = new List<Tuple<int, Rhino.Geometry.Curve, double>>();

            for (int n = 0; n < count; n++)
            {
                indexedC.Add(Tuple.Create(n, C[n], C[n].PointAtLength(t).X));
            }
            // Sort the indexedC list based on the lengths
            indexedC.Sort((a, b) => a.Item3.CompareTo(b.Item3));

            Rhino.Geometry.Curve[] crvs = new Rhino.Geometry.Curve[count];
            int[] index = new int[count];

            for (int m = 0; m < count; m++)
            {
                crvs[m] = indexedC[m].Item2; // Accessing the Curve from the tuple
                index[m] = indexedC[m].Item1; // Accessing the index from the tuple
            }

            //Output
            DA.SetDataList(0, crvs);
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
                var ImageBytes = Moth.Properties.Resources.SortX;
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
            get { return new Guid("9759E8D4-29AC-42EE-9292-F8CAC1099A7F"); }
        }
    }
}