using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry.Collections;
using System.Linq;

namespace Moth.Properties
{
    public class GaussianCurvatureComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GaussianCurvatureComponent class.
        /// </summary>
        public GaussianCurvatureComponent()
          : base("Gaussian Curvature", "GaussianK",
              "Gaussian curvature estimation of mesh vertices",
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
            pManager.AddNumberParameter("GaussianK", "Kg", "Gaussian curvature", GH_ParamAccess.list);
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

            //Error Handling: Make sure Mesh is triangular
            if (!IsTriangularMesh(M))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mesh M must be triangular");
                return;
            }

            //Functions

            // Check if the mesh is triangular
            bool IsTriangularMesh(Mesh mesh)
            {
                foreach (var face in mesh.Faces)
                {
                    if (!face.IsTriangle)
                    {
                        return false;
                    }
                }
                return true;
            }

            //Get Sum of Angles of Faces that belong to the vertex
            double SumAngle(int vertexidx, MeshFaceList Faces, MeshTopologyVertexList Vertices)
            {
                // Get the connected faces for the given vertex index
                int[] ConnectedFaces = Vertices.ConnectedFaces(vertexidx);
                //Iterate through every connected face
                double Sumang = 0;
                for (int j = 0; j < ConnectedFaces.Length; j++)
                {
                    MeshFace neighbour = Faces[ConnectedFaces[j]];
                    //List of vertices on the face
                    List<Point3d> verts = new List<Point3d> { Vertices[neighbour.A], Vertices[neighbour.B], Vertices[neighbour.C] };
                    //Get point3d of the vertexidx
                    Point3d Vertex = Vertices[vertexidx];
                    Sumang += GetAngle(Vertex, verts);
                }

                return Sumang;
            }
            // Get Angle of Vertex in Face
            double GetAngle(Point3d Vertex, List<Point3d> FaceVerts)
            {
                //filter out the specified vertex
                List<Point3d> otherPoints = FaceVerts.Where(v => v != Vertex).ToList();
                //Vectors for angle
                Vector3d v1 = otherPoints[0] - Vertex;
                Vector3d v2 = otherPoints[1] - Vertex;
                // Normalize vectors
                v1.Unitize();
                v2.Unitize();
                // Calculate the angle between the two vectors using the dot product
                double dotProduct = Vector3d.Multiply(v1, v2);
                double angle = Math.Acos(dotProduct);
                //Return result
                return angle;
            }

            //Get Baricentric Area
            double BaricentricArea(int vertexidx, MeshFaceList Faces, MeshTopologyVertexList Vertices)
            {
                // Get the connected faces for the given vertex index
                int[] ConnectedFaces = Vertices.ConnectedFaces(vertexidx);
                //Get neighbour triangles Areas
                double SumAreas = 0;
                foreach (var fc in ConnectedFaces)
                {
                    //Acess neighbour vertices
                    MeshFace neighbour = Faces[fc];
                    List<Point3d> verts = new List<Point3d> { Vertices[neighbour.A], Vertices[neighbour.B], Vertices[neighbour.C] };
                    //Get area
                    SumAreas += TriangleArea(verts);
                }
                return SumAreas / 3;
            }

            //Get Triangle Area
            double TriangleArea(List<Point3d> vertices)
            {
                // Vectors AB and AC
                double[] AB = { vertices[1].X - vertices[0].X, vertices[1].Y - vertices[0].Y, vertices[1].Z - vertices[0].Z };
                double[] AC = { vertices[2].X - vertices[0].X, vertices[2].Y - vertices[0].Y, vertices[2].Z - vertices[0].Z };

                // Cross product AB x AC
                double[] crossProduct = {
                  AB[1] * AC[2] - AB[2] * AC[1],
                  AB[2] * AC[0] - AB[0] * AC[2],
                  AB[0] * AC[1] - AB[1] * AC[0]
                  };

                // Magnitude of the cross product
                double crossProductMagnitude = Math.Sqrt(
                  crossProduct[0] * crossProduct[0] +
                  crossProduct[1] * crossProduct[1] +
                  crossProduct[2] * crossProduct[2]
                  );

                // Area of the triangle
                double area = 0.5 * crossProductMagnitude;
                return area;

            }

            //Algorithm
            // Get the topology vertices and faces
            MeshFaceList topologyFaces = M.Faces;
            MeshTopologyVertexList topologyVertices = M.TopologyVertices;
            MeshTopologyEdgeList topologyEdges = M.TopologyEdges;

            //Gaussian Curvature
            List<double> GaussianK = new List<double>();
            double edgeAdjustmentFactor = 0.01; // Adjust this factor as needed
            for (int i = 0; i < topologyVertices.Count; i++)
            {
                // Check if the vertex is on the edge
                bool isEdgeVertex = topologyVertices.ConnectedEdges(i).Any(e => topologyEdges.GetConnectedFaces(e).Length == 1);

                // Get Baricentric Area
                double BarArea = BaricentricArea(i, topologyFaces, topologyVertices);
                // Get Sum of Angles of Faces that belong to the vertex
                double Sumangle = SumAngle(i, topologyFaces, topologyVertices);
                // Get Curvature
                double curvature = (2 * Math.PI - Sumangle) / BarArea;

                if (isEdgeVertex)
                {
                    // Apply edge adjustment factor to the curvature
                    curvature *= edgeAdjustmentFactor;
                }

                GaussianK.Add(curvature);
            }

            //Output
            DA.SetDataList(0, GaussianK);
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
                var ImageBytes = Moth.Properties.Resources.GaussianK;
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
            get { return new Guid("B85EE6EC-1536-426A-81ED-43B5E6DD7646"); }
        }
    }
}