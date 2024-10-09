using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Grasshopper.Kernel.Data;
using System.ComponentModel;
using System.Linq;

namespace Moth
{
    public class RoadsLineComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Roads class.
        /// </summary>
        public RoadsLineComponent()
          : base("Roads", "Roads",
              "Offsets curve to create roads",
              "Moth", "Curve")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Lines", "L", "List of lines", GH_ParamAccess.list);
            pManager.AddNumberParameter("Width", "w", "Road width", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Smoothnes", "s", "Road smoothness", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Roads", "R", "Offseted curves", GH_ParamAccess.list);
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
            double w = 1;
            DA.GetData(1, ref w);
            int S = 0;
            DA.GetData(2, ref S);

            //Error Handling: Check if Curves are Planar on XY
            for (int c = 0; c < C.Count; c++)
            {
                if (C[c].IsPlanar() == false)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curves must be linear");
                    return;
                }
            }
            //Error Handling: Check if Curves are Lines
            for (int c = 0; c < C.Count; c++)
            {
                if (!C[c].IsLinear())
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "L must be lines");
                    return;
                }
            }

            //Functions
            //Reparametrize Curve function
            Rhino.Geometry.Curve ReparametrizeCurve(Rhino.Geometry.Curve curv)
            {
                // Create a copy of the curve to avoid modifying the original
                Rhino.Geometry.Curve reparametrizedCurve = curv.DuplicateCurve();

                // Set the new domain from 0 to 1
                Interval newDomain = new Interval(0.0, 1.0);

                // Change the domain of the curve
                reparametrizedCurve.Domain = newDomain;

                return reparametrizedCurve;
            }

            //Algorithm
            //Set edge style
            int n = S % 3;

            //Define XY plane for the curve offset
            Point3d origin = new Point3d(0, 0, 0);
            Vector3d normal = new Vector3d(0, 0, 1);
            Plane plane = new Plane(origin, normal);

            //Empty list of curves
            List<Rhino.Geometry.Curve> crvs = new List<Rhino.Geometry.Curve>();
            //Create Empty List of Regions
            List<Rhino.Geometry.Curve> regions = new List<Rhino.Geometry.Curve>();

            //Offset Curves
            for (int i = 0; i < C.Count; i++)
            {
                //Offset
                crvs.AddRange(C[i].Offset(plane, w / 2, 0.001, CurveOffsetCornerStyle.Sharp));
                crvs.AddRange(C[i].Offset(plane, -w / 2, 0.001, CurveOffsetCornerStyle.Sharp));
            }
            //Join the Curves
            Rhino.Geometry.Curve[] cr = Rhino.Geometry.Curve.JoinCurves(crvs);

            //Create Empty List of the curves of the region
            List<Rhino.Geometry.Curve> cc = new List<Rhino.Geometry.Curve>();
            //Offset Curves Sharp
            //Offset Curves Round or sharp
            if (n == 1 || n == 0)
            {

                for (int j = 0; j < cr.Length; j += 2)
                {
                    cc.Clear();
                    // Create an empty list to hold a pair of curves
                    //List<Curve> cc = new List<Curve>();

                    // Add the pair of curves to the list
                    cc.Add(cr[j]);
                    cc.Add(cr[j + 1]);

                    // Calculate midpoints for the arcs
                    Point3d mStart = new Point3d(
                      (cr[j].PointAtStart.X + cr[j + 1].PointAtStart.X) / 2,
                      (cr[j].PointAtStart.Y + cr[j + 1].PointAtStart.Y) / 2,
                      (cr[j].PointAtStart.Z + cr[j + 1].PointAtStart.Z) / 2
                      );

                    Point3d mEnd = new Point3d(
                      (cr[j].PointAtEnd.X + cr[j + 1].PointAtEnd.X) / 2,
                      (cr[j].PointAtEnd.Y + cr[j + 1].PointAtEnd.Y) / 2,
                      (cr[j].PointAtEnd.Z + cr[j + 1].PointAtEnd.Z) / 2
                      );

                    //Vectors
                    Vector3d vec1 = cr[j].TangentAt(cr[j].Domain[0]) * -1;
                    Vector3d vec2 = cr[j].TangentAt(cr[j].Domain[1]);
                    vec1.Unitize();
                    vec2.Unitize();

                    //Calculate Radius
                    double radius = w / 2;

                    //Movemidpoints
                    Point3d Pt1 = mStart + vec1 * radius;
                    Point3d Pt2 = mEnd + vec2 * radius;


                    // Create arcs between the start points and the midpoints
                    Arc arc1 = new Arc(cr[j].PointAtStart, Pt1, cr[j + 1].PointAtStart);
                    Arc arc2 = new Arc(cr[j].PointAtEnd, Pt2, cr[j + 1].PointAtEnd);

                    // Create ArcCurve instances
                    ArcCurve arcCurve1 = new ArcCurve(arc1);
                    ArcCurve arcCurve2 = new ArcCurve(arc2);

                    //ROUND
                    if (n == 1)
                    {

                        // Add the arc curves to the list
                        cc.Add(arcCurve1);
                        cc.Add(arcCurve2);

                        // Join the curves to form a region
                        regions.AddRange(Rhino.Geometry.Curve.JoinCurves(cc));
                    }

                    //Sharp
                    else if (n == 0)
                    {
                        LineCurve ln1 = new LineCurve(arcCurve1.PointAtStart, arcCurve1.PointAtEnd);
                        LineCurve ln2 = new LineCurve(arcCurve2.PointAtStart, arcCurve2.PointAtEnd);
                        cc.Add(ln1);
                        cc.Add(ln2);
                        //Join Curves and Create a Region
                        regions.AddRange(Rhino.Geometry.Curve.JoinCurves(cc));
                    }
                }
            }

            //Offset Curves Chamfer
            else if (n == 2)
            {
                for (int j = 0; j < cr.Length; j += 2)
                {
                    cc.Clear();
                    //Create Empty List of the curves of the region
                    //List<Curve> cc = new List<Curve>();

                    //Calculate chamfer distance
                    double distance = w * 0.25;

                    //Compute the directions
                    Vector3d Sdir1 = cr[j].TangentAt(cr[j].Domain[0]);
                    Vector3d Sdir2 = cr[j + 1].PointAtStart - cr[j].PointAtStart;
                    Sdir1.Unitize();
                    Sdir2.Unitize();

                    Vector3d Edir1 = cr[j].TangentAt(cr[j].Domain[1]) * -1;
                    Vector3d Edir2 = cr[j + 1].PointAtEnd - cr[j].PointAtEnd;
                    Edir1.Unitize();
                    Edir2.Unitize();


                    //Calculate Points
                    Point3d SPt1 = cr[j].PointAtStart + Sdir1 * distance;
                    Point3d SPt2 = cr[j].PointAtStart + Sdir2 * distance;
                    Point3d SPt3 = cr[j + 1].PointAtStart + Sdir1 * distance;
                    Point3d SPt4 = cr[j + 1].PointAtStart - Sdir2 * distance;

                    Point3d EPt1 = cr[j].PointAtEnd + Edir1 * distance;
                    Point3d EPt2 = cr[j].PointAtEnd + Edir2 * distance;
                    Point3d EPt3 = cr[j + 1].PointAtEnd + Edir1 * distance;
                    Point3d EPt4 = cr[j + 1].PointAtEnd - Edir2 * distance;


                    //Calculate Lines at edges
                    LineCurve Sln1 = new LineCurve(SPt1, SPt2);
                    LineCurve Sln2 = new LineCurve(SPt2, SPt4);
                    LineCurve Sln3 = new LineCurve(SPt4, SPt3);

                    cc.Add(Sln1);
                    cc.Add(Sln2);
                    cc.Add(Sln3);

                    LineCurve Eln1 = new LineCurve(EPt1, EPt2);
                    LineCurve Eln2 = new LineCurve(EPt2, EPt4);
                    LineCurve Eln3 = new LineCurve(EPt4, EPt3);


                    cc.Add(Eln1);
                    cc.Add(Eln2);
                    cc.Add(Eln3);

                    //Calculate lines without the trimmed ends
                    if (cr[j].IsPolyline() == false)
                    {
                        //Get intervals for trimming
                        double len1 = cr[j].GetLength();
                        double len2 = cr[j + 1].GetLength();
                        Interval intl = new Interval(distance / len1, 1 - distance / len1);
                        Interval int2 = new Interval(distance / len2, 1 - distance / len2);

                        //Reparamettrize the curves
                        Rhino.Geometry.Curve c1 = ReparametrizeCurve(cr[j]);
                        Rhino.Geometry.Curve c2 = ReparametrizeCurve(cr[j + 1]);

                        //Add to list
                        cc.Add(c1.Trim(intl));
                        cc.Add(c2.Trim(int2));

                        regions.AddRange(cc);
                    }

                    else if (cr[j].IsPolyline() == true)
                    {
                        //Get intervals for trimming
                        double len1 = cr[j].GetLength();
                        double len2 = cr[j + 1].GetLength();

                        // Calculate the lengths to trim from both ends of each polyline
                        double trim1;
                        cr[j].ClosestPoint(SPt1, out trim1);
                        double trim2;
                        cr[j].ClosestPoint(EPt1, out trim2);
                        double trim3 = distance;
                        cr[j + 1].ClosestPoint(SPt3, out trim3);
                        double trim4;
                        cr[j + 1].ClosestPoint(EPt3, out trim4);

                        // Create intervals for trimming
                        Interval intl = new Interval(trim1, trim2);
                        Interval int2 = new Interval(trim3, trim4);

                        //Add to list
                        cc.Add(cr[j].Trim(intl));
                        cc.Add(cr[j + 1].Trim(int2));

                        regions.AddRange(cc);
                    }

                }

            }

            //Transforms polylines to NurbsCurve Instances
            List<Rhino.Geometry.Curve> nurbs = new List<Rhino.Geometry.Curve>();
            for (int j = 0; j < regions.Count; j++)
            {
                if (regions[j].IsPolyline())
                {
                    nurbs.Add(regions[j].ToNurbsCurve());
                }
                else
                {
                    nurbs.Add(regions[j]);
                }
            }

            // Join curves
            List<Rhino.Geometry.Curve> joinedCurves = new List<Rhino.Geometry.Curve>(Rhino.Geometry.Curve.JoinCurves(nurbs, 0.008));

            // Initialize a list to hold the cumulative union result
            List<Rhino.Geometry.Curve> cumulativeUnionResult = new List<Rhino.Geometry.Curve>();
            // Sort curves based on bounding box
            joinedCurves = joinedCurves.OrderBy(c => c.GetBoundingBox(true).Min).ToList();

            int batchSize = 2; // Adjust based on how many you want to union at once
            for (int i = 0; i < joinedCurves.Count; i += batchSize)
            {
                // Create a batch of curves
                List<Rhino.Geometry.Curve> batch = joinedCurves.Skip(i).Take(batchSize).ToList();

                // Perform a union on the batch
                Rhino.Geometry.Curve[] union = Rhino.Geometry.Curve.CreateBooleanUnion(batch, 0.001);

                if (union != null && union.Length > 0)
                {
                    cumulativeUnionResult.AddRange(union);
                }
            }

            Rhino.Geometry.Curve[] finalUnion = Rhino.Geometry.Curve.CreateBooleanUnion(cumulativeUnionResult, 0.001);
            if (finalUnion != null && finalUnion.Length > 0)
            {
                cumulativeUnionResult.Clear();
                cumulativeUnionResult.AddRange(finalUnion);
            }

            //Output
            DA.SetDataList(0, finalUnion);
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
                var ImageBytes = Moth.Properties.Resources.Roads;
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
            get { return new Guid("33369998-2357-4AE6-BD55-C71B31B23B75"); }
        }
    }
}