using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;

namespace Moth
{
    public class KochSnowflakeComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the KochSnowflakeComponent class.
        /// </summary>
        public KochSnowflakeComponent()
          : base("KochSnowflakeComponent", "KSnowflake",
              "Koch snowflake fractal curve",
              "Moth", "Curve")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "P", "Polyline", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Iterations", "i", "Number of iterations", GH_ParamAccess.item, 3);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Snowflake", "S", "Koch snowflake", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Inputs
            Curve C = null;
            if (!DA.GetData(0, ref C)) return;
            int i = 3;
            if (!DA.GetData(1, ref i)) return;

            //Error handling
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

            //Functions
            List<Polyline> recursive(Polyline Poly, int iterations, List<Polyline> List)
            {
                //Run recursion
                if (iterations > 0)
                {
                    Polyline NewPoly = Koch(Poly);
                    iterations -= 1;
                    recursive(NewPoly, iterations, List);

                    if (iterations == 0)
                    {
                        List.Add(NewPoly);
                    }
                }

                //Return result
                return List;
            }



            Polyline Koch(Polyline Poly)
            {
                //Create empty List of curves
                List<Curve> Curves = new List<Curve>();

                //Create list of lines in polyline
                int numberL = Poly.SegmentCount;
                List<Line> Lines = new List<Line>();
                for (int m = 0; m < numberL; m++)
                {
                    Lines.Add(Poly.SegmentAt(m));
                }

                //Iterate through each line to generate the new polylines
                foreach (Line ln in Lines)
                {
                    //End and start point
                    Point3d Start = ln.From;
                    Point3d End = ln.To;
                    //Length
                    double Length = ln.Length;
                    //Midpoint, 1/3 point and 2/3 point
                    Point3d Midpt = ln.PointAtLength(Length / 2);
                    Point3d Pt1 = ln.PointAtLength(Length / 3);
                    Point3d Pt2 = ln.PointAtLength(Length * 2 / 3);

                    //Get normal vector
                    double dX = End.X - Start.X;
                    double dY = End.Y - Start.Y;
                    double dZ = End.Z - Start.Z;

                    Vector3d Vector = new Vector3d(dY, -dX, -dZ);
                    Vector.Unitize();

                    //New Lines
                    List<Point3d> Points = new List<Point3d> { Start, Pt1, Midpt + (Vector * Length / 5), Pt2, End };
                    Polyline NewPoly = new Polyline(Points);

                    Curves.Add(NewPoly.ToNurbsCurve());
                }

                //Joined the curves
                Curve[] Joined = Curve.JoinCurves(Curves);
                // Transform into Polylines
                List<Polyline> Polylines = new List<Polyline>();
                foreach (Curve crv in Joined)
                {
                    Polyline polyline;
                    if (crv.TryGetPolyline(out polyline))
                    {
                        Polylines.Add(polyline);
                    }
                }

                // Return result
                return Polylines[0];
            }


            //Algorithm
            //Create empty list
            List<Polyline> Lst = new List<Polyline>();
            List<Polyline> Snowflake = recursive(P, i, Lst);

            //Output
            DA.SetDataList(0, Snowflake);

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
                var ImageBytes = Moth.Properties.Resources.KochSnowflake;
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
            get { return new Guid("5C8AB7B4-5CDB-4AD9-AA8D-6CF12A6A79EE"); }
        }
    }
}