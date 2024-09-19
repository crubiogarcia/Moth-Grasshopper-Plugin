using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System.Linq;
using System.IO;
using Grasshopper.Kernel.Data;
using Grasshopper;

namespace Moth
{
    public class MeshFaceClusteringComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MeshFaceClusteringComponent class.
        /// </summary>
        public MeshFaceClusteringComponent()
          : base("Mesh Face Clustering with K Means ++", "MFaceClustering",
              "Triangular mesh face clustering by K Means ++ Algorithm",
              "Moth", "Mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddIntegerParameter("n clusters", "n", "Number of clusters", GH_ParamAccess.item, 3);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshFaceParameter("Face clusters", "F", "Face clusters", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Clusters of indices", "i", "Clusters of indices", GH_ParamAccess.tree);
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
            int n = 3;
            if (!DA.GetData(1, ref n)) return;

            //Error Handling: Make sure Mesh is triangular
            if (!IsTriangularMesh(M))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mesh M must be triangular");
                return;
            }

            //Functions
            double[] SelectWeightedRandom(List<double[]> faceData, double[] probabilities, int Sd)
            {
                // Start Random
                Random rand = new Random(Sd);
                double randomValue = rand.NextDouble();
                double cumulative = 0.0;
                for (int i = 0; i < probabilities.Length; i++)
                {
                    cumulative += probabilities[i];
                    if (randomValue < cumulative)
                    {
                        return faceData[i];
                    }
                }
                return faceData.Last(); // Fallback
            }
            //Get Distance
            double Distance(double[] PtA, double[] PtB)
            {
                //If Points have not the same length
                if (PtA.Length != PtB.Length)
                {
                    throw new ArgumentException("The input arrays must have the same length.");
                }

                double sumOfSquares = 0;

                for (int i = 0; i < PtA.Length; i++)
                {
                    sumOfSquares += Math.Pow(PtA[i] - PtB[i], 2);
                }

                return Math.Sqrt(sumOfSquares);

            }

            //Get Mesh Faces Data
            List<double[]> GetFaceData(MeshFaceList Facs, MeshVertexList Verts)
            {
                //Create empty list of Data
                List<double[]> FData = new List<double[]>();
                //Add info to the list
                foreach (MeshFace face in Facs)
                {
                    //Get vertices
                    Point3d PtA = Verts[face.A];
                    Point3d PtB = Verts[face.B];
                    Point3d PtC = Verts[face.C];

                    //Draw Edges
                    Line LnA = new Line(PtA, PtB);
                    Line LnB = new Line(PtB, PtC);
                    Line LnC = new Line(PtC, PtA);

                    //GetVectors
                    Vector3d VcA = LnA.Direction;
                    VcA.Unitize();
                    Vector3d VcB = LnB.Direction;
                    VcB.Unitize();
                    Vector3d VcC = LnB.Direction;
                    VcC.Unitize();

                    //Get Angles
                    double Angle1 = GetAngle(VcA, VcB);
                    double Angle2 = GetAngle(VcB, VcC);
                    double Angle3 = GetAngle(VcC, VcA);

                    //Get Edge Lengths
                    double Length1 = LnA.Length;
                    double Length2 = LnB.Length;
                    double Length3 = LnC.Length;

                    //Add to Data List
                    double[] Data = { Angle1, Angle2, Angle3, Length1, Length2, Length3 };
                    FData.Add(Data);
                }

                return FData;
            }

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
            //Calculate Angle
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

            //Get centroid from cluster of points
            double[] GetCentroid(List<double[]> Points)
            {
                //Get Dimension
                int PointDimension = Points[0].Length;
                //Sum all coordinates
                double[] Sum = new double[PointDimension];
                //Get Coordinates
                for (int k = 0; k < PointDimension; k++)
                {
                    foreach (double[] pt in Points)
                    {
                        Sum[k] += pt[k];
                    }
                }

                // Initialize the centroid array to the correct length
                double[] Centroid = new double[PointDimension];

                // Divide each element of the sum array by the number of points to get the centroid
                for (int i = 0; i < PointDimension; i++)
                {
                    Centroid[i] = Sum[i] / Points.Count;
                }

                //Return Point
                return Centroid;
            }

            //Algorithm
            // Get the mesh faces and vertices
            MeshFaceList Faces = M.Faces;
            MeshVertexList Vertices = M.Vertices;

            //Create tree
            DataTree<double[]> KTree = new DataTree<double[]>();
            DataTree<int> Indx = new DataTree<int>();

            //Get Face Data
            List<double[]> FaceData = GetFaceData(Faces, Vertices);

            //Random centroids
            List<double[]> Centroids = new List<double[]>();
            int Seed = 25;
            Random random = new Random(Seed);
            int firstIndex = random.Next(FaceData.Count);
            Centroids.Add(FaceData[firstIndex]);

            //Select the remaining k-1 centroids
            for (int m = 1; m < n; m++)
            {
                // Calculate distances from each data point to the nearest centroid
                double[] minDistances = new double[FaceData.Count];
                for (int j = 0; j < FaceData.Count; j++)
                {
                    double minDistance = double.MaxValue;
                    foreach (var centroid in Centroids)
                    {
                        double distance = Distance(FaceData[j], centroid);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                        }
                    }
                    minDistances[j] = minDistance * minDistance; // Square the distance
                }

                // Calculate probabilities
                double sumOfDistances = minDistances.Sum();
                double[] probabilities = minDistances.Select(d => d / sumOfDistances).ToArray();

                // Select the next centroid based on the calculated probabilities
                double[] nextCentroid = SelectWeightedRandom(FaceData, probabilities, Seed);
                Centroids.Add(nextCentroid);
            }

            //Start Counter
            int Counter = 0;
            int Iterations = 100;

            //Start KMeans Calculation
            while (Counter < Iterations)
            {
                //Clear Tree
                KTree.Clear();
                Indx.Clear();

                //Iterate through all the points
                for (int t = 0; t < FaceData.Count; t++)
                {
                    //Start dictionary
                    Dictionary<int, double> Distances = new Dictionary<int, double>();
                    for (int j = 0; j < Centroids.Count; j++)
                    {
                        //Calculate distance
                        double distance = Distance(FaceData[t], Centroids[j]);

                        // Add distance to the dictionary
                        Distances[j] = distance;
                    }


                    //Calculate closest centroid
                    int closestCentroid = Distances.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;

                    //Add Point to Branch of Closest Centroid
                    GH_Path path = new GH_Path(closestCentroid);
                    KTree.Add(FaceData[t], path);
                    Indx.Add(t, path);
                }

                //Clear Centroids
                Centroids.Clear();
                //Create new centroids
                for (int k = 0; k < n; k++)
                {
                    if (k >= 0 && k < KTree.BranchCount)
                    {
                        double[] Pt = GetCentroid(KTree.Branch(k));
                        Centroids.Add(Pt);
                    }
                }
                ///Add Counter
                Counter += 1;

            }

            //Empty List of Mesh Faces
            DataTree<MeshFace> FaceCluster = new DataTree<MeshFace>();
            for (int m = 0; m < Indx.BranchCount; m++)
            {
                //Add Path
                GH_Path path = new GH_Path(m);
                foreach (int index in Indx.Branch(m))
                {
                    FaceCluster.Add(Faces[index], path);
                }
            }


            //Output
            DA.SetDataTree(0, FaceCluster);
            DA.SetDataTree(1, Indx);

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
                var ImageBytes = Moth.Properties.Resources.MeshFaceClustering;
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
            get { return new Guid("B9A5B1A8-FF08-4253-B610-A03231FA9CAF"); }
        }
    }
}