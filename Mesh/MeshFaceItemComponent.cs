using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System.Linq;
using System.IO;

namespace Moth
{
    public class MeshFaceItemComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MeshFaceItemComponent class.
        /// </summary>
        public MeshFaceItemComponent()
          : base("Mesh Face Item", "MFace",
              "Extracts meshface i from mesh",
              "Moth", "Mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Index", "i", "Mesh face index", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Edges", "E", "Edges of face", GH_ParamAccess.list);
            pManager.AddPointParameter("Vertices", "V", "Vertices of face", GH_ParamAccess.list);
            pManager.AddVectorParameter("Normal", "N", "Normal vector of face", GH_ParamAccess.item);
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
            int i = 0;
            if (!DA.GetData(1, ref i)) return;

            //Algorithm

            // Get the mesh faces and vertices
            MeshFaceList Faces = M.Faces;
            M.FaceNormals.ComputeFaceNormals();
            MeshFaceNormalList Normals = M.FaceNormals;
            MeshVertexList MVertices = M.Vertices;

            //Error Handling
            if (i > Faces.Count - 1)
            {

                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "i must be inside the faces range");

            }


            //Get Face Normal
            Vector3d Normal = Normals[i];

            MeshFace Face = Faces[i];
            //Get Face Vertices
            List<Point3d> FVertices = new List<Point3d>();
            FVertices.Add(MVertices[Face.A]);
            FVertices.Add(MVertices[Face.B]);
            FVertices.Add(MVertices[Face.C]);

            // If the face is a quad, add the fourth vertex
            if (Face.IsQuad)
            {
                FVertices.Add(MVertices[Face.D]);
            }

            //Get Edges
            List<Line> Edges = new List<Line>();
            for (int n = 0; n < FVertices.Count; n++)
            {
                Line edge = new Line(FVertices[n], FVertices[(n + 1) % FVertices.Count]);
                Edges.Add(edge);

            }

            //Output
            DA.SetDataList(0, Edges);
            DA.SetDataList(1, FVertices);
            DA.SetData(2, Normal);
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
                var ImageBytes = Moth.Properties.Resources.MeshFaceItem;
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
            get { return new Guid("5637D2B8-82F9-4009-B78B-B159C1DC99ED"); }
        }
    }
}