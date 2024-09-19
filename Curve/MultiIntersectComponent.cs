using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Grasshopper.Kernel.Data;

namespace Moth
{
    public class MultiIntersectComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MultiTrim class.
        /// </summary>
        public MultiIntersectComponent()
          : base("MultiInterserct", "MIntersect",
              "Intersect a set of curves within themselves",
              "Moth", "Curve")
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
            pManager.AddGenericParameter("Intersections", "I", "Intersections", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Parameters", "t", "Parameters of intersections", GH_ParamAccess.tree);
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

            //Algorithm
            //Set tolerance for intersection
            double tolerance = 0.001;
            int ccount = C.Count;

            //Empty data tree for points and overlaps
            DataTree<Object> elemtree = new DataTree<Object>();
            //Empty data tree for parameters
            DataTree<Object> ttree = new DataTree<Object>();

            //Intersections
            for (int i = 0; i < ccount; i++)
            {
                for (int j = i + 1; j < ccount; j++)
                {
                    //get the intersection
                    Rhino.Geometry.Intersect.CurveIntersections intersect = Rhino.Geometry.Intersect.Intersection.CurveCurve(C[i], C[j], tolerance, tolerance * 2);

                    //Build the tree paths
                    GH_Path path1 = new GH_Path(i, j);
                    GH_Path path2 = new GH_Path(j, i);

                    //if they intersect
                    if (intersect.Count > 0)
                    {
                        //if the intersection is a point
                        for (int m = 0; m < intersect.Count; m++)
                        {
                            if (intersect[m].IsPoint)
                            {
                                //add points
                                elemtree.Add(intersect[m].PointA, path1);
                                elemtree.Add(intersect[m].PointB, path2);
                                //add parameter in curves of points
                                ttree.Add(intersect[m].ParameterA, path1);
                                ttree.Add(intersect[m].ParameterB, path2);
                            }
                            //if the intersection is an overlap
                            else if (intersect[m].IsOverlap)
                            {
                                //Get overlap interval
                                Interval intervalA = intersect[m].OverlapA;
                                Interval intervalB = intersect[m].OverlapB;

                                //Get overlap segments of curve
                                elemtree.Add(C[i].Trim(intervalA), path1);
                                elemtree.Add(C[j].Trim(intervalB), path2);

                                //add  interval start and end parameters of the overlap
                                ttree.Add(intervalA, path1);
                                ttree.Add(intervalB, path2);
                            }
                        }
                    }
                    //if they dont intersect
                    else
                    {
                        elemtree.Add(null, path1);
                        elemtree.Add(null, path2);
                    }
                }
            }

            //Output
            DA.SetDataTree(0, elemtree);
            DA.SetDataTree(1, ttree);
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
                var ImageBytes = Moth.Properties.Resources.MultiIntersect;
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
            get { return new Guid("DF497CFA-5D51-4F34-8B56-0B65A9A74272"); }
        }
    }
}