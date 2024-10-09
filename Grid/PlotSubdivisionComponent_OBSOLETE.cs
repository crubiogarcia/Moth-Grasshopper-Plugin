﻿using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using System.Linq;

namespace Moth
{
    public class PlotSubdivisionComponent_OBSOLETE : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PlotSubdivisionComponent class.
        /// </summary>
        public PlotSubdivisionComponent_OBSOLETE()
          : base("PlotSubdivisionComponent", "PSubdivision",
              "Recursively subdivides polygon generating a random grid with straight lines",
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
            Rhino.Geometry.Curve C = null;
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
            List<Rhino.Geometry.Curve> Subd(List<Rhino.Geometry.Curve> crvs, double min, double sd)
            {
                // Convert the double seed to an integer seed
                int seed = sd.GetHashCode();
                //Empty List of curves
                List<Rhino.Geometry.Curve> NewCrvs = new List<Rhino.Geometry.Curve>();

                foreach (var crv in crvs)
                {

                    // Create brep with the curve
                    Brep[] brep = Brep.CreatePlanarBreps(crv, 0.001);

                    //Get edges of curve
                    //Get edges of curve
                    Rhino.Geometry.Curve[] edges = Edges(crv);
                    //Reparametrize edges
                    List<Rhino.Geometry.Curve> ReEdges = new List<Rhino.Geometry.Curve>();
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
                        Rhino.Geometry.Curve extendedCurve = lin.Extend(CurveEnd.Both, 4000, CurveExtensionStyle.Line);

                        //Split the surface
                        Brep[] SplitBreps = brep[0].Split(new List<Rhino.Geometry.Curve> { extendedCurve }, 0.001);

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
                        Rhino.Geometry.Curve[] edgs = brp.DuplicateEdgeCurves();
                        NewCrvs.AddRange(Rhino.Geometry.Curve.JoinCurves(edgs));
                    }

                    //New seed
                    seed = seed / (seed + 23);
                }

                //Return result
                return NewCrvs;

            }

            //Recursion
            List<Rhino.Geometry.Curve> recursion(List<Rhino.Geometry.Curve> list, List<Rhino.Geometry.Curve> emptyList, int iterations, double seed, double length)
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
            Rhino.Geometry.Curve[] Edges(Rhino.Geometry.Curve crv)
            {
                //Get Edges
                Rhino.Geometry.Curve[] edges = crv.DuplicateSegments();

                //If edges are larger than 0
                if (edges.Length > 0)
                {
                    return edges;
                }
                //If curve doesnt have edges
                else
                {
                    Rhino.Geometry.Curve[] curve = { crv };
                    return edges;
                }
            }

            //Reparametrize
            Rhino.Geometry.Curve ReparametrizeCurve(Rhino.Geometry.Curve curve)
            {
                // Create a copy of the curve to avoid modifying the original
                Rhino.Geometry.Curve reparametrizedCurve = curve.DuplicateCurve();

                // Set the new domain from 0 to 1
                Interval newDomain = new Interval(0.0, 1.0);

                // Change the domain of the curve
                reparametrizedCurve.Domain = newDomain;

                return reparametrizedCurve;
            }


            //Algorithm
            //List of curves
            List<Rhino.Geometry.Curve> Curves = new List<Rhino.Geometry.Curve> { C };
            //Empty List
            List<Rhino.Geometry.Curve> empty = new List<Rhino.Geometry.Curve>();
            List<Rhino.Geometry.Curve> subD = recursion(Curves, empty, I, S, L);

            //Output
            DA.SetDataList(0, subD);


        }

        //Set exposure level
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var ImageBytes = Moth.Properties.Resources.PlotSubdivision;
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
            get { return new Guid("E4BBCEB3-3B9F-4007-A1F5-DF3F59296ADC"); }
        }
    }
}