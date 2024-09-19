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

namespace Moth
{
    public class MeanCurvatureComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MeanCurvatureComponent class.
        /// </summary>
        public MeanCurvatureComponent()
          : base("Mean Curvature", "MeanK",
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
            pManager.AddNumberParameter("MeanK", "Km", "Mean curvature", GH_ParamAccess.list);
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

            //Get angle from non adjacent edges
            double GetAngle(List<Line> Edges, Line SelectedLine)
            {
                //Get edges that are not the EdgeLine
                //Create empty list
                List<Line> NonAdjacentedges = new List<Line>();
                for (int n = 0; n < Edges.Count; n++)
                {
                    if (IsSameEdge(Edges[n], SelectedLine) == false)
                    {
                        NonAdjacentedges.Add(Edges[n]);
                    }
                }

                //Get angle between non adjacent edges
                // Calculate the angle between the two vectors using the dot product
                double dotProduct = Vector3d.Multiply(NonAdjacentedges[0].Direction, NonAdjacentedges[1].Direction);
                double angle = Math.Acos(dotProduct);
                if (double.IsNaN(angle))
                {
                    angle = 0;
                }
                return angle;
            }


            //Check if edges are the same
            bool IsSameEdge(Line edge1, Line edge2)
            {
                return (edge1.From == edge2.From && edge1.To == edge2.To) || (edge1.From == edge2.To && edge1.To == edge2.From);
            }

            //Get edges of meshFace
            List<Line> FaceEdges(MeshFace face, MeshTopologyVertexList Vertices)
            {
                //Initialize empty list
                List<Line> Edges = new List<Line>();
                // Retrieve the vertices of the face
                int[] faceVertices = { face.A, face.B, face.C };
                for (int i = 0; i < 3; i++)
                {
                    Point3d startVertex = Vertices[faceVertices[i]];
                    Point3d endVertex = Vertices[faceVertices[(i + 1) % 3]];
                    Line edge = new Line(startVertex, endVertex);
                    Edges.Add(edge);
                }

                return Edges;
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


            //Mean Curvature
            List<double> MeanK = new List<double>();

            //for(int idx =2310; idx<2311; idx++)
            for (int idx = 0; idx < topologyVertices.Count; idx++)
            {
                //Get edges in the vertex
                int[] edges = topologyVertices.ConnectedEdges(idx);

                //Get faces in the vertex
                int[] faces = topologyVertices.ConnectedFaces(idx);
                //Initialize Sum of Cotangent Weights
                double SumCotgWeight = 0;
                // Get the Cotangent weight of every edge and sum it
                foreach (int j in edges)
                {
                    //Initialize sum of cotangents
                    double CotgSum = 0;
                    //Get adjacent faces
                    int[] adjacentFaces = topologyEdges.GetConnectedFaces(j);
                    Line EdgeLine = topologyEdges.EdgeLine(j);
                    //Get cotangent per face adjacent to edge
                    foreach (int FaceID in adjacentFaces)
                    {
                        //Get Face
                        MeshFace Face = topologyFaces[FaceID];
                        // Retrieve the vertices of the face
                        List<Line> Edges = FaceEdges(Face, topologyVertices);
                        //Get angle between non adjacent edges
                        double angle = GetAngle(Edges, EdgeLine);

                        //Get cotangent between non adjacent edges
                        double Cotg;
                        if (angle != 0)
                        {
                            Cotg = 1.0 / Math.Tan(angle);
                        }
                        else
                        {
                            Cotg = 0;
                        }

                        //Add the contangent to the sum
                        CotgSum += Cotg;

                    }

                    //Get edgeline length
                    double EdgeLength = EdgeLine.Length;

                    //Calculate Cotangent Weights for that edge
                    double CotgWeight = EdgeLength * CotgSum;
                    SumCotgWeight = +CotgWeight;
                }

                // Get Baricentric Area
                double BarArea = BaricentricArea(idx, topologyFaces, topologyVertices);

                //Get mean curvature of vertex
                double VertexMeanK = SumCotgWeight / (2 * BarArea);
                MeanK.Add(VertexMeanK);
            }

            //Output
            DA.SetDataList(0, MeanK);
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
                var ImageBytes = Moth.Properties.Resources.MeanK;
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
            get { return new Guid("8B087E69-7337-484D-9C5F-39AE68423871"); }
        }
    }
}