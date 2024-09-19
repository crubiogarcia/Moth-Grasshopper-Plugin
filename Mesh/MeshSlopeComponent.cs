using System;
using System.Collections.Generic;
using System.IO;
using Eto.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace Moth
{
    public class MeshSlopeComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MeshSlopeComponent class.
        /// </summary>
        public MeshSlopeComponent()
          : base("Mesh Slope", "MSlope",
              "Slope of triangular mesh faces",
              "Moth", "Mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Angle", "A", "Angle of each face", GH_ParamAccess.list);
            pManager.AddNumberParameter("Slope %", "S", "Slope percentage of each face", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Input
            Mesh M = null;
            if (!DA.GetData(0, ref M)) return;

            //Functions
            double GetAngle(Vector3d a, Vector3d b)
            {
                // Calculate the angle between the two vectors using the dot product
                double dotProduct = Vector3d.Multiply(a, b);
                double angle = Math.Acos(dotProduct);
                if (double.IsNaN(angle))
                {
                    angle = 0;
                }
                return angle;
            }

            //Algorithm
            MeshVertexList MeshVertices = M.Vertices;

            //Start empty lists
            List<double> slopes = new List<double>();
            List<double> angles = new List<double>();

            for (int i = 0; i < MeshVertices.Count; i++)
            {
                // Compute the normal at the vertex
                Vector3d normal = M.Normals[i];
                normal.Unitize();

                Vector3d projected = new Vector3d(normal.X, normal.Y, 0);

                double angle = GetAngle(normal, projected);
                if (angle > Math.PI)
                {
                    angle = GetAngle(projected, normal);
                }
                double slope = Math.Atan(Math.PI * 0.5 - angle) * 100;

                angles.Add(angle);
                slopes.Add(slope);

            }

            //Output
            DA.SetDataList(0, angles);
            DA.SetDataList(1, slopes);
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
                var ImageBytes = Moth.Properties.Resources.MeshSlope;
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
            get { return new Guid("CA480896-6582-4799-9222-255CC35032BB"); }
        }
    }
}