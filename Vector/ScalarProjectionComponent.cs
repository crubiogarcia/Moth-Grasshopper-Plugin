using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;

namespace Moth
{
    public class ScalarProjectionComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ScalarProjectionComponent()
          : base("Scalar Projection", "ScalarP",
              "Scalar Projection of A into B",
              "Moth", "Vector")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddVectorParameter("Vector", "A", "Vector A", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vector", "B", "Vector A", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("scalar projection", "P", "Projection", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Inputs
            Vector3d A = new Vector3d(0, 0, 0);
            if (!DA.GetData(1, ref  A)) return;

            Vector3d B = new Vector3d(0, 0, 0);
            if (!DA.GetData(1, ref B)) return;

            //Funtions
            //ScalarProjection
            double ScalarProjection(Vector3d v1, Vector3d v2)
            {
                double dotProduct = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
                double lengthB = v2.Length; // Calculate the length of vector B
                if (lengthB == 0)
                {
                    throw new ArgumentException("Vector B length is zero, cannot divide by zero.");
                }
                double scalarP = dotProduct / lengthB;

                return scalarP;
            }

            //Algorithm
            double ScalarP = ScalarProjection(A, B);

            //Output
            DA.SetData(0, ScalarP);

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
                var ImageBytes = Moth.Properties.Resources.ScalarProjection;
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
            get { return new Guid("789B6CC1-2429-4CE1-8D2F-5C526FF25B15"); }
        }
    }
}