using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using System.Security.Cryptography;

namespace Moth
{
    public class CleanPolyAngleGreaterThanComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CleanPolyline class.
        /// </summary>
        public CleanPolyAngleGreaterThanComponent()
          : base("Clean Poly Angle Greater", "CPoly G",
              "Reduces segments in a polyline where the angle between consecutive segments is greater than the specified tolerance",
              "Moth", "Curve")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "Polyline", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angle", "a", "Angle tolerance in radians", GH_ParamAccess.item, 2.00);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "R", "Reduced polyline", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Inputs
            Rhino.Geometry.Curve C = null;
            DA.GetData(0, ref C);
            double a = 0.15;
            DA.GetData(1, ref a);

            //Algorithm

            // Convert polyline points to a list

            Polyline P;
            if (C.TryGetPolyline(out P))
            {
                // The curve was successfully converted to a polyline
            }
            else
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "P must be a polyline");
                return;
            }


            List<Point3d> pts = new List<Point3d>(P.ToArray());



            // Initialize a flag to track if any point was removed in the last iteration
            bool pointRemoved = true;

            // Continue looping while points are being removed
            while (pointRemoved)
            {
                pointRemoved = false;

                // Iterate through the points and check angles between consecutive segments
                for (int i = 1; i < pts.Count - 1; i++)
                {
                    Vector3d vec1 = pts[i] - pts[i - 1];
                    Vector3d vec2 = pts[i + 1] - pts[i];

                    double angle = Vector3d.VectorAngle(vec1, vec2);

                    // Remove the point if the angle is less than the threshold
                    if (angle > a)
                    {
                        pts.RemoveAt(i);
                        pointRemoved = true;
                        break; // Exit the loop to restart checking from the beginning
                    }
                }
            }

            // Check if the polyline is closed
            if (pts.Count > 2 && P.IsClosed)
            {
                // Calculate the angle between the last and first segments
                Vector3d lastSegment = pts[pts.Count - 1] - pts[pts.Count - 2];
                Vector3d firstSegment = pts[1] - pts[0];

                double angle = Vector3d.VectorAngle(lastSegment, firstSegment);

                // Remove both the first and last points if the angle is greater than the threshold
                if (angle > a)
                {
                    pts.RemoveAt(pts.Count - 1); // Remove last point
                    pts.RemoveAt(0); // Remove first point
                    pointRemoved = true;
                }
            }

            // Create a new polyline from the filtered points
            Polyline filteredPolyline = new Polyline(pts);


            if (P.IsClosed && !filteredPolyline.IsClosed)
            {
                pts.Add(pts[0]);
            }

            Polyline CRV = new Polyline(pts);

            //Output
            DA.SetData(0, CRV);

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
                var ImageBytes = Moth.Properties.Resources.CleanPolylineGreater;
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
            get { return new Guid("d6b3afc2-5402-4897-9e29-6a37e19874a1"); }
        }
    }
}