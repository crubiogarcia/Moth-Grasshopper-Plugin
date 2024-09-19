using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using System.Linq;

namespace Moth
{
    public class PlotSubdivisionAngleComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PlotSubdivisionwithAngleComponent class.
        /// </summary>
        public PlotSubdivisionAngleComponent()
          : base("PlotSubdivisionAngleComponent", "PSubdivisionAngle",
              "Recursively subdivides polygon with lines rotated generating a random grid with angled lines",
              "Moth", "Grid")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Closed Curve", GH_ParamAccess.item);
            pManager.AddNumberParameter("Minimum length", "L", "Minimun plot segment length of plot", GH_ParamAccess.item, 3);
            pManager.AddIntegerParameter("Iterations", "I", "Number of iterations", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("Seed", "S", "Random seed", GH_ParamAccess.item, 5);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Plots", "G", "Lines with plot subdivisions", GH_ParamAccess.list);
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

            double L = 3;
            if (!DA.GetData(1, ref L)) return;

            int I = 3;
            if (!DA.GetData(2, ref I)) return;

            double S = 5;
            if (!DA.GetData(3, ref S)) return;

            
            //Error handling
            if (C.IsClosed == false)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curves must be closed");
                return;
            }
            //Error Handling: Check if Curves are Planar on XY
            if (C.IsPlanar() == false)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curves must be planar");
                return;
            }

            
            //Functions
            //Subdivision
            List<Curve> Subd(List<Curve> crvs, double min, double sd)
            {
                // Convert the double seed to an integer seed
                int seed = sd.GetHashCode();
                //Empty List of curves
                List<Curve> NewCrvs = new List<Curve>();

                foreach (var crv in crvs)
                {

                    // Create brep with the curve
                    Brep[] brep = Brep.CreatePlanarBreps(crv, 0.001);

                    //Get edges of curve
                    //Get edges of curve
                    Curve[] edges = Edges(crv);
                    //Reparametrize edges
                    List<Curve> ReEdges = new List<Curve>();
                    if (edges.Length > 0)
                    {
                        for (int j = 0; j < edges.Length; j++)
                        {
                            ReEdges.Add(ReparametrizeCurve(edges[j]));
                        }
                    }
                    else
                    {
                        ReEdges.Add(ReparametrizeCurve(crv));
                    }


                    //Get a list of lengths
                    List<double> lengths = new List<double>();
                    for (int i = 0; i < ReEdges.Count; i++)
                    {
                        lengths.Add(ReEdges[i].GetLength());
                    }

                    // Find the maximum length and its index
                    double maxLength = lengths.Max();
                    int maxIndex = lengths.IndexOf(maxLength);

                    //Create empty brep list
                    List<Brep> NewBreps = new List<Brep>();


                    //If max length is larger than minimun edge split
                    if (maxLength > min * 2.1 && maxLength != min)
                    {
                        //Get normalized min and max parameters in curve
                        double start = min / maxLength;
                        double end = 1 - min / maxLength;

                        //Get a random choice parameter inside the range established
                        // Initialize the random number generator with the seed
                        Random random = new Random(seed);
                        // Generate a random number between start and end
                        double randomValue = random.NextDouble() * (end - start) + start;
                        // Round the random value to 2 decimal places
                        double parameter = Math.Round(randomValue, 2);

                        //Get Point in Max Side Length and Normal Vector
                        //Get Point
                        Point3d pt = ReEdges[maxIndex].PointAt(parameter);
                        //Get Tangent vector
                        Vector3d tangent = ReEdges[maxIndex].TangentAt(parameter);
                        //Get Normal Vector to Tangent by rotating tangent
                        Vector3d axis = new Vector3d(0, 0, 1);
                        double angle = Math.PI / 2;
                        //Rotate tangent
                        tangent.Rotate(angle, axis);

                        //Draw line to split elements with normal direction
                        LineCurve lin = new LineCurve(pt, pt + tangent);
                        Curve extendedCurve = lin.Extend(CurveEnd.Both, 4000, CurveExtensionStyle.Line);
                        //Intersect Curve
                        Rhino.Geometry.Intersect.CurveIntersections intersect = Rhino.Geometry.Intersect.Intersection.CurveCurve(crv, extendedCurve, 0.001, 0.001);
                        //if they intersect
                        List<Point3d> intPt = new List<Point3d>();
                        if (intersect.Count > 0)
                        {
                            //if the intersection is a point
                            for (int m = 0; m < intersect.Count; m++)
                            {
                                //Add parameters
                                if (intersect[m].IsPoint)
                                {
                                    intPt.Add(intersect[m].PointA);
                                }
                            }
                        }
                        //Create Line inside crv
                        LineCurve nwline = new LineCurve(intPt[0], intPt[1]);
                        Curve NewLine = ReparametrizeCurve(nwline);
                        //Split the curve at the specified parameter
                        Curve[] splitCurves = NewLine.Split(0.5);
                        //Rotate First Part of the splitcurves
                        Vector3d vect = new Vector3d(splitCurves[1].PointAtEnd - splitCurves[1].PointAtStart);

                        //Generate a random number between for the angle
                        double ang1 = -0.1 * Math.PI;
                        double ang2 = 0.1 * Math.PI;
                        new Random(seed + 1);
                        double randomAng = ang1 + random.NextDouble() * (ang2 - ang1);
                        double Angle = Math.Round(randomAng, 2);
                        //Vector Axis
                        Vector3d axs = new Vector3d(0, 0, 1);
                        //Randomly select if rotated or nor
                        int randomChoice = random.Next(2);
                        if (randomChoice == 0)
                        {
                            Angle = 0;
                        }
                        //Rptate
                        vect.Rotate(Angle, axs);
                        //Build Line with new direction
                        Line RotatedLine = new Line(splitCurves[0].PointAtEnd, vect, 400);
                        Curve RLine = RotatedLine.ToNurbsCurve();
                        Curve extended = splitCurves[0].Extend(CurveEnd.Start, 4000, CurveExtensionStyle.Line);
                        List<Curve> crvss = new List<Curve> { extended, RLine };
                        Curve[] polycrv = Curve.JoinCurves(crvss);
                        //Split the surface
                        Brep[] SplitBreps = brep[0].Split(crvss, 0.001);

                        //Add if the new surfaces were split to the list of new surfaces
                        if (SplitBreps != null)
                        {
                            NewBreps.AddRange(SplitBreps);
                        }
                        else
                        {
                            NewBreps.Add(brep[0]);
                        }
                    }

                    //Else (If max length is NOT larger than minimun edge split)
                    else
                    {
                        NewBreps.Add(brep[0]);
                    }

                    //Append New Brep bounding Curve
                    foreach (var brp in NewBreps)
                    {
                        Curve[] edgs = brp.DuplicateEdgeCurves();
                        NewCrvs.AddRange(Curve.JoinCurves(edgs));
                    }

                    //New seed
                    seed = seed / (seed + 23);
                }

                //Return result
                return NewCrvs;

            }

            //Recursion
            List<Curve> recursion(List<Curve> list, List<Curve> emptyList, int iterations, double seed, double length)
            {
                if (iterations > 0)
                {
                    int sd = seed.GetHashCode() * 12 / 14;
                    list = Subd(list, length, sd);
                    iterations -= 1;

                    recursion(list, emptyList, iterations, seed, length);

                    if (iterations == 0)
                    {
                        emptyList.AddRange(list);
                    }
                }

                return emptyList;
            }

            //Get curve edges
            Curve[] Edges(Curve crv)
            {
                //Get Edges
                Curve[] edges = crv.DuplicateSegments();

                //If edges are larger than 0
                if (edges.Length > 0)
                {
                    return edges;
                }
                //If curve doesnt have edges
                else
                {
                    Curve[] curve = { crv };
                    return edges;
                }
            }

            //Reparametrize curve
            Curve ReparametrizeCurve(Curve curve)
            {
                // Create a copy of the curve to avoid modifying the original
                Curve reparametrizedCurve = curve.DuplicateCurve();

                // Set the new domain from 0 to 1
                Interval newDomain = new Interval(0.0, 1.0);

                // Change the domain of the curve
                reparametrizedCurve.Domain = newDomain;

                return reparametrizedCurve;
            }

            //Algorithm
            //List of curves
            List<Curve> Curves = new List<Curve> { C };
            //Empty List
            List<Curve> empty = new List<Curve>();
            List<Curve> subD = recursion(Curves, empty, I, S, L);

            //Output
            DA.SetDataList(0, subD);
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
                var ImageBytes = Moth.Properties.Resources.PlotSubdivisionWithAngle;
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
            get { return new Guid("78B67D02-139E-4AB9-8A1E-2B31EC119D1B"); }
        }
    }
}