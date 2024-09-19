using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;

namespace Moth
{
    public class VectorProjectionComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the VectorProjectionComponent class.
        /// </summary>
        public VectorProjectionComponent()
          : base("VectorProjectionComponent", "Vector Projection",
              "Vector Projection of A into B",
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
            pManager.AddVectorParameter("Vector", "V", "Projected vector", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Inputs
            Vector3d A = new Vector3d(0, 0, 0);
            if (!DA.GetData(1, ref A)) return;

            Vector3d B = new Vector3d(0, 0, 0);
            if (!DA.GetData(1, ref B)) return;

            //Functions
            //Vector Projection
            Vector3d VectorProjection(Vector3d v1, Vector3d v2)
            {
                double dotProduct = v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
                double lengthB = v2.Length;
                if (lengthB == 0)
                {
                    throw new ArgumentException("Vector v2 length is zero, cannot divide by zero.");
                }
                double Value = dotProduct / (lengthB * lengthB);

                Vector3d VectorP = Value * v2;

                return VectorP;
            }

            //Algorithm
            Vector3d VP = VectorProjection(A, B);

            //Output
            DA.SetData(0, VP);

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
                var ImageBytes = Moth.Properties.Resources.VectorProjection;
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
            get { return new Guid("6992F94C-0744-4856-88A1-04CE3510F0A6"); }
        }
    }
}