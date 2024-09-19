using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;

namespace Moth
{
    public class KMeansPointsComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the KMeansClusteringPointComponent class.
        /// </summary>
        public KMeansPointsComponent()
          : base("KMeansClusteringPointComponent", "K-Means++ Clustering Point",
              "Point clustering through K-Means++ algorithm",
              "Moth", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("List of Points", "P", "List of points", GH_ParamAccess.list);
            pManager.AddIntegerParameter("n clusters", "n", "Number of clusters", GH_ParamAccess.item, 3);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Clusters of points", "C", "Clusters of points", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Clusters of indices", "i", "Clusters of indices", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Input
            List<Point3d> P = new List<Point3d>();
            if (!DA.GetDataList(0, P)) return;

            int n = 3;
            if (!DA.GetData(1, ref n)) return;

            //Functions
            Point3d SelectWeightedRandom(List<Point3d> Points, double[] probabilities, int Sd)
            {
                // Start Random
                Random randm = new Random(Sd);
                double randomValue = randm.NextDouble();
                double cumulative = 0.0;
                for (int i = 0; i < probabilities.Length; i++)
                {
                    cumulative += probabilities[i];
                    if (randomValue < cumulative)
                    {
                        return Points[i];
                    }
                }
                return Points.Last(); // Fallback
            }

            List<double> GetCoordinates(List<Point3d> Points, String str)
            {
                //Initialize dictionary
                List<double> Lst = new List<double>();

                //Iterate through the points
                if (str == "X")
                {
                    foreach (Point3d Point in Points)
                    {
                        Lst.Add(Point.X);
                    }
                }

                else if (str == "Y")
                {
                    foreach (Point3d Point in Points)
                    {
                        Lst.Add(Point.Y);
                    }
                }

                //Iterate through the points
                if (str == "Z")
                {
                    foreach (Point3d Point in Points)
                    {
                        Lst.Add(Point.Z);
                    }
                }

                return Lst;
            }

            

            Point3d GetCentroid(List<Point3d> Points)
            {
                //Get X,Y,Z Coordinates
                List<double> XCord = GetCoordinates(Points, "X");
                List<double> YCord = GetCoordinates(Points, "Y");
                List<double> ZCord = GetCoordinates(Points, "Z");

                //Sum all coordinates
                double XSum = 0;
                double YSum = 0;
                double ZSum = 0;

                // Iterate through the list and add each number to the sum
                for (int i = 0; i < Points.Count; i++)
                {
                    XSum += XCord[i];
                    YSum += YCord[i];
                    ZSum += ZCord[i];
                }

                double X = XSum / Points.Count;
                double Y = YSum / Points.Count;
                double Z = ZSum / Points.Count;

                //Create Point
                Point3d Point = new Point3d(X, Y, Z);

                //Return Point
                return Point;
            }

            //Algorithm
            //Create tree
            DataTree<Point3d> KTree = new DataTree<Point3d>();
            DataTree<int> Indx = new DataTree<int>();

            //Random centroids
            List<Point3d> Centroids = new List<Point3d>();
            int Seed = 25;
            Random random = new Random(Seed);
            int firstIndex = random.Next(P.Count);
            Centroids.Add(P[firstIndex]);

            //Select the remaining k-1 centroids
            for (int m = 1; m < n; m++)
            {
                // Calculate distances from each data point to the nearest centroid
                double[] minDistances = new double[P.Count];
                for (int j = 0; j < P.Count; j++)
                {
                    double minDistance = double.MaxValue;
                    foreach (Point3d centroid in Centroids)
                    {
                        double distance = P[j].DistanceTo(centroid);
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
                Point3d nextCentroid = SelectWeightedRandom(P, probabilities, Seed);
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
                for (int t = 0; t < P.Count; t++)
                {
                    //Start dictionary
                    Dictionary<int, double> Distances = new Dictionary<int, double>();
                    for (int j = 0; j < Centroids.Count; j++)
                    {
                        //Calculate distance
                        double distance = P[t].DistanceTo(Centroids[j]);

                        // Add distance to the dictionary
                        Distances[j] = distance * distance;
                    }

                    //Calculate closest centroid
                    int closestCentroid = Distances.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
                    //Add Point to Branch of Closest Centroid
                    GH_Path path = new GH_Path(closestCentroid);
                    KTree.Add(P[t], path);
                    Indx.Add(t, path);
                }

                //Clear Centroids
                Centroids.Clear();
                //Create new centroids
                for (int k = 0; k < n; k++)
                {
                    Point3d Pt = GetCentroid(KTree.Branch(k));
                    Centroids.Add(Pt);
                }
                //Add Counter
                Counter += 1;
            }

            //Output
            DA.SetDataTree(0, KTree);
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
                var ImageBytes = Moth.Properties.Resources.KMeansPoints;
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
            get { return new Guid("0ED312DC-20A9-4096-8595-3C0EEF7797E9"); }
        }
    }
}