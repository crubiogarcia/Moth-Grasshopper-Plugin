using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Moth.Curves
{
    public class MultiSplitComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MultiTrimComponent class.
        /// </summary>
        public MultiSplitComponent()
          : base("MultiSplit", "MSplit",
              "Splits a list of curves based on their intersections",
              "Moth", "Curve")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "List of curves to split", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Splitted Curves", "S", "List of splitted curves", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Get input curves
            List<Rhino.Geometry.Curve> curves = new List<Rhino.Geometry.Curve>();
            if (!DA.GetDataList(0, curves)) return;

            //Algorithm

            // Store intersection parameters for each curve
            List<List<double>> splitParameters = new List<List<double>>();

            // Initialize split parameters for each curve
            for (int i = 0; i < curves.Count; i++)
            {
                splitParameters.Add(new List<double>());
            }

            // Find intersections between all pairs of curves
            for (int i = 0; i < curves.Count; i++)
            {
                for (int j = 0; j < curves.Count; j++)
                {
                    if (i != j) // Ensure the same curve is not intersected with itself
                    {
                        var intersectionEvents = Intersection.CurveCurve(curves[i], curves[j], 0.001, 0.001);

                        if (intersectionEvents != null && intersectionEvents.Count > 0)
                        {
                            foreach (var intersection in intersectionEvents)
                            {
                                // Add the intersection parameters for both curves
                                splitParameters[i].Add(intersection.ParameterA);
                            }
                        }
                    }
                }
            }

            // List to store all split segments
            List<Rhino.Geometry.Curve> allSplitSegments = new List<Rhino.Geometry.Curve>();

            // Split each curve at its collected intersection points
            for (int i = 0; i < curves.Count; i++)
            {
                var splitSegments = curves[i].Split(splitParameters[i]);
                if (splitSegments != null && splitSegments.Length > 0)
                {
                    allSplitSegments.AddRange(splitSegments);
                }
                else
                {
                    allSplitSegments.Add(curves[i]); // Add original curve if no splits
                }
            }

            // Output the split segments
            DA.SetDataList(0, allSplitSegments);
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
                var ImageBytes = Moth.Properties.Resources.MultiSplit;
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
            get { return new Guid("59F6E920-C3DA-40C8-9922-C9619F504C2E"); }
        }
    }
}