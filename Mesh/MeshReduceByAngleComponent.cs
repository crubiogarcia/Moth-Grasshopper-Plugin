using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System.Linq;
using System.IO;

namespace Moth
{
    public class MeshReducEByAngleComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MeshReductionByAngleComponent class.
        /// </summary>
        public MeshReducEByAngleComponent()
          : base("Mesh Reduction by Angle", "MRAngle",
              "Mesh reduction of flat areas with face normal differential angle as a threshold",
              "Moth", "Mesh")

        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angle", "a", "Angle treshold", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Reduced vertices", "V", "Reduced vertices", GH_ParamAccess.list);
            pManager.AddMeshParameter("Reduced mesh", "R", "Reduced mesh", GH_ParamAccess.item);
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

            double a = 0;
            DA.GetData(1, ref a);

            //Functions
            
            //Calculate Angle
            double GetAngle(Vector3d a1, Vector3d a2)
            {
                // Calculate the angle between the two vectors using the dot product
                double dotProduct = Vector3d.Multiply(a1, a2);
                double angle = Math.Acos(dotProduct);
                if (double.IsNaN(angle))
                {
                    angle = 0;
                }
                return angle;
            }

            //Algorithm

            // Get the mesh faces and vertices
            MeshFaceList Faces = M.Faces;
            M.FaceNormals.ComputeFaceNormals();
            MeshFaceNormalList Normals = M.FaceNormals;
            MeshVertexList Vertices = M.Vertices;

            // Start list of faces to remove
            List<int> FacesToRemove = new List<int>();

            for (int i = 0; i < Faces.Count; i++)
            {
                // Get neighbor faces
                int[] Neighbours = Faces.AdjacentFaces(i);

                // Get face normal
                Vector3d FaceNormal = Normals[i];
                FaceNormal.Unitize();

                // Get list of angles between face normal and its neighbors
                List<double> Angles = new List<double>();

                // Get angles with neighbors
                foreach (int n in Neighbours)
                {
                    Vector3d nV = Normals[n];
                    nV.Unitize();
                    double angle = GetAngle(FaceNormal, nV);
                    Angles.Add(angle);
                }

                // Get average angle
                double AverageAngle = Angles.Average();

                // Check if average is below threshold
                if (AverageAngle < a)
                {
                    FacesToRemove.AddRange(Neighbours);
                }
            }

            // Remove faces from the mesh in one go, after the loop
            List<int> uniqueFacesToRemove = FacesToRemove.Distinct().ToList();
            uniqueFacesToRemove.Sort((c, b) => b.CompareTo(c)); // Sort in descending order

            foreach (int faceRemove in uniqueFacesToRemove)
            {
                Faces.RemoveAt(faceRemove, true);
            }


            // Output
            DA.SetDataList(0, Vertices);
            DA.SetData(1, M);
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
                var ImageBytes = Moth.Properties.Resources.MeshReduce;
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
            get { return new Guid("4A0E01CC-A6E4-439F-A6EF-708B97A85A6B"); }
        }
    }
}