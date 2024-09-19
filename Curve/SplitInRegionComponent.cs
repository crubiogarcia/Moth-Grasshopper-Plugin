using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using System.ComponentModel;
using System.Linq;

namespace Moth
{
    public class SplitInRegionComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TrimInsideRegion class.
        /// </summary>
        public SplitInRegionComponent()
          : base("SplitInRegion", "SplitR",
              "Split curves with region",
              "Moth", "Curve")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "List of curves", GH_ParamAccess.list);
            pManager.AddCurveParameter("Region", "R", "Region to split curves", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves inside", "I", "Curves inside region", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curves outside", "O", "Curves outside region", GH_ParamAccess.list);

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
            Curve R = null;
            DA.GetData(1, ref R);

            //Error handling
            //Error Handling if C is not planar
            if (R.IsPlanar() == false)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve R must be planar");
                return;
            }
            //Error Handling if C is not closed
            if (R.IsClosed == false)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve R must be closed");
                return;
            }
            //Error Handling if curves in A are not planar
            for (int r = 0; r < C.Count; r++)
            {
                if (C[r].IsPlanar() == false)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curves C must be closed");
                    return;
                }
            }

            //Functions
            //Reparametrize Curve function
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
            double tolerance = 0.001;
            //Set empty list curves
            List<Curve> crvs = new List<Curve>();
            List<Interval> test = new List<Interval>();
            List<double> testt = new List<double>();
            //Reparametrize curve
            List<Curve> rcrvs = new List<Curve>();
            for (int j = 0; j < C.Count; j++)
            {
                rcrvs.Add(ReparametrizeCurve(C[j]));
            }

            //Get intersection parameters on every curve
            for (int i = 0; i < rcrvs.Count; i++)
            {
                //get intersection event
                Rhino.Geometry.Intersect.CurveIntersections intersect = Rhino.Geometry.Intersect.Intersection.CurveCurve(rcrvs[i], R, tolerance, tolerance);

                //empty list of parameters
                List<double> paramt = new List<double>();

                //if they intersect
                if (intersect.Count > 0)
                {
                    //if the intersection is a point
                    for (int m = 0; m < intersect.Count; m++)
                    {
                        //Add parameters
                        if (intersect[m].IsPoint)
                        {
                            paramt.Add(intersect[m].ParameterA);
                        }
                    }
                    //Sort parameters in list
                    paramt.Sort((a, b) => a.CompareTo(b));
                    //Add 0 and 1 to the list
                    paramt.Insert(0, 0);
                    paramt.Add(1);
                    //TODO: Create intervals for every parameter and shatter curves
                    for (int t = 0; t < paramt.Count - 1; t++)
                    {
                        Interval inte = new Interval(paramt[t], paramt[t + 1]);
                        crvs.Add(rcrvs[i].Trim(inte));
                        test.Add(inte);
                    }
                }
            }

            //Get mid points in the new shattered curves
            List<Point3d> mid = new List<Point3d>();
            for (int n = 0; n < crvs.Count; n++)
            {
                //Get length
                double curveLength = crvs[n].GetLength();
                //Get parameter add half length
                double midpointParameter;
                crvs[n].LengthParameter(curveLength / 2.0, out midpointParameter);
                //Add midpoint to list
                mid.Add(crvs[n].PointAt(midpointParameter));
            }

            //Create two empty lists
            List<Curve> interior = new List<Curve>();
            List<Curve> exterior = new List<Curve>();

            //Define a plane for the method curve.contains
            Point3d origin = new Point3d(0, 0, 0);
            Vector3d normal = new Vector3d(0, 0, 1);
            Plane plane = new Plane(origin, normal);

            //Check if points are inside curve and add those curves to the exterior or interior lists
            for (int k = 0; k < mid.Count; k++)
            {
                //Check if the Curve Contains the point
                PointContainment ptt = R.Contains(mid[k], plane, 0.001);
                if (ptt == PointContainment.Inside)
                {
                    interior.Add(crvs[k]);
                }
                else if (ptt == PointContainment.Outside)
                {
                    exterior.Add(crvs[k]);
                }
            }


            //Outputs
            DA.SetDataList(0, interior);
            DA.SetDataList(1, exterior);

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
                var ImageBytes = Moth.Properties.Resources.SplitInRegion;
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
            get { return new Guid("23964E1E-9182-4E30-8257-3A62DF2EC1FE"); }
        }
    }
}