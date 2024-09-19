using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Linq;
using System.Security.Cryptography;

namespace Moth
{
    public class RecursiveCellPartitioningComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the RecursiveCellPartitioning class.
        /// </summary>
        public RecursiveCellPartitioningComponent()
          : base("RecursiveCellPartitioning", "RCellPart",
              "Recursive cell partitioning from sorted grid",
              "Moth", "Grid")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Sorted Grid", "G", "sorted grid input(tree of rows/columns)", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("MaxSize", "M", "Max size of cell cluster", GH_ParamAccess.item, 3);
            pManager.AddIntegerParameter("Seed", "S", "Random seed", GH_ParamAccess.item, 5);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Cells", "C", "Clustered Cells", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Inputs
            // Define a GH_Structure to hold the data tree
            GH_Structure<GH_Curve> G = new GH_Structure<GH_Curve>();
            // Load the input tree using GetDataTree (specific for tree access)
            if (!DA.GetDataTree(0, out G)) return;
            int M = 3;
            if (!DA.GetData(1, ref M)) return;
            int S = 5;
            if (!DA.GetData(2, ref S)) return;

            //Functions
            //Calcuate distances
            List<double> Distances(Point3d v, List<Point3d> vertices)
            {
                var distances = new List<double>();
                foreach (var vertex in vertices)
                {
                    double distance = Math.Sqrt(Math.Pow(vertex.X - v.X, 2) + Math.Pow(vertex.Y - v.Y, 2));
                    distances.Add(distance);
                }
                return distances;
            }
            //Check Conditionx
            List<int
              > CheckConditions(List<int[]> list1, List<int[]> list2, Func<int, int, bool> operatorA, Func<int, int, bool> operatorB, bool equal1, bool equal2)
            {
                //Create empty list
                List<int> toRemove = new List<int>();

                if (equal2 == false)
                {
                    for (int i = 0; i < list2.Count; i++)
                    {
                        for (int t = 0; t < list1.Count; t++)
                        {
                            if (operatorA(list1[t][0], list2[i][0]))
                            {
                                if (equal1 == false && operatorB(list1[t][1], list2[i][1]))
                                {
                                    toRemove.Add(t);
                                }
                                else if (equal1)
                                {
                                    toRemove.Add(t);
                                }
                            }
                        }
                    }
                }
                else if (equal2 == true)
                {
                    for (int i = 0; i < list2.Count; i++)
                    {
                        for (int t = 0; t < list1.Count; t++)
                        {
                            if (operatorA(list1[t][1], list2[i][1]))
                            {
                                toRemove.Add(t);
                            }
                        }
                    }
                }
                return toRemove;
            }
            //Get neighbours coordinates of an input cell
            List<int[]> RoundNeighbours(int[] cellCoordinates, List<int[]> neighboursCoordinates, int step)
            {
                List<int[]> n = new List<int[]>();

                foreach (int[] i in neighboursCoordinates)
                {
                    if (i[0] == cellCoordinates[0] + step || i[0] == cellCoordinates[0] - step)
                    {
                        if (cellCoordinates[1] - step <= i[1] && i[1] <= cellCoordinates[1] + step)
                        {
                            n.Add(i);
                        }
                    }
                    else if (i[1] == cellCoordinates[1] + step || i[1] == cellCoordinates[1] - step)
                    {
                        if (cellCoordinates[0] - step <= i[0] && i[0] <= cellCoordinates[0] + step)
                        {
                            n.Add(i);
                        }
                    }
                }
                return n;
            }
            List<int[]> GetNeighbours(int[] coordinates, int[] gridsize, int NeighNumber)
            {
                //Set empty list of coordinates
                List<int[]> neighbours = new List<int[]>();
                //Set min and max values for the neighbours coordinates
                int min_x = NeighNumber - 1, min_y = NeighNumber - 1;
                int max_x = gridsize[0] - (1 + NeighNumber), max_y = gridsize[1] - (1 + NeighNumber);
                for (int i = -NeighNumber + 1; i < NeighNumber; i++)
                {
                    for (int j = -NeighNumber + 1; j < NeighNumber; j++)
                    {
                        //Set cells
                        int x = coordinates[0] + i;
                        int y = coordinates[1] + j;
                        //Check that coordinates are inside grid boundaries
                        if (0 <= x && x <= gridsize[0] - 1 && 0 <= y && y <= gridsize[1] - 1)
                        {
                            neighbours.Add(new int[] { x, y });
                        }
                    }
                }

                return neighbours;
            }

            //Algorithm

            //Create tree of cells with the input G
            DataTree<Cell> Nested = new DataTree<Cell>();
            //Store the grid size
            GH_Path pth = new GH_Path(0);
            int[] GridSize = { G.PathCount, G[pth].Count };
            //Create a queue list and a list of true or false for visited
            DataTree<bool> Visited = new DataTree<bool>();
            List<Curve> Qu = new List<Curve>();
            for (int i = 0; i < GridSize[0]; i++)
            {
                GH_Path path = new GH_Path(i);
                for (int j = 0; j < GridSize[1]; j++)
                {
                    GH_Curve ghCurve = G[path][j] as GH_Curve;
                    if (ghCurve == null || ghCurve.Value == null) continue; // Handle null values
                    Curve crv = ghCurve.Value;
                    
                    Qu.Add(crv);
                    Visited.Add(false, new GH_Path(i));
                    Cell newCell = new Cell(crv);
                    Nested.Add(newCell, new GH_Path(i));

                }
            }


            //Create an index list
            List<int> Counter = Enumerable.Range(0, GridSize[0] * GridSize[1]).ToList();
            //Create empty list of Curves
            List<Curve> Rectangles = new List<Curve>();
            //Convert seed to int
            int Seed = (int)S;

            while (Counter.Count > 0)
            {

                Random random = new Random(Seed); // Seed the random number generator
                int index = random.Next(Counter.Count());
                int ix = Counter[index];
                Counter.RemoveAt(index);

                // Translate index into the nested list indexes
                int x = ix / GridSize[1];
                int y = ix % GridSize[1];



                // Get Cell Coordinates
                int[] cord = new int[] { x, y };
                //Get Cell neighbours
                List<int[]> neighbours = GetNeighbours(cord, GridSize, M);

                // Check if any neighbours are visited
                // Create list
                List<bool> check = new List<bool>(neighbours.Count);
                for (int i = 0; i < neighbours.Count; i++)
                {
                    check.Add(false);
                }

                var larger = new Dictionary<string, List<int[]>>()
        {
          { "larger", new List<int[]>() },
          { "smaller", new List<int[]>() },
          { "equal", new List<int[]>() }
          };

                var smaller = new Dictionary<string, List<int[]>>()
        {
          { "larger", new List<int[]>() },
          { "smaller", new List<int[]>() },
          { "equal", new List<int[]>() }
          };

                var equal = new Dictionary<string, List<int[]>>()
        {
          { "larger", new List<int[]>() },
          { "smaller", new List<int[]>() }
          };

                //Create list of elements to be removed
                List<int> to_remove = new List<int>();

                for (int j = 0; j < (M - 1); j++)
                {
                    //Get closest neighbours
                    List<int[]> RoundNeigh = RoundNeighbours(cord, neighbours, j + 1);

                    //Append to list if visited
                    for (int k = 0; k < RoundNeigh.Count; k++)
                    {
                        int[] roundNeigh = RoundNeigh[k];
                        int neighIndex = neighbours.FindIndex(n => n.SequenceEqual(roundNeigh));
                        if (neighIndex == -1) continue; // If the round neighbor is not in the neighbors list, skip it

                        GH_Path path = new GH_Path(roundNeigh[0]);

                        if (Visited.Branch(path)[roundNeigh[1]] == true)
                        {
                            check[neighIndex] = true;

                            if (roundNeigh[0] > cord[0] && roundNeigh[1] > cord[1])
                            {
                                larger["larger"].Add(roundNeigh);
                            }

                            else if (roundNeigh[0] > cord[0] && roundNeigh[1] < cord[1])
                            {
                                larger["smaller"].Add(RoundNeigh[k]);
                            }

                            else if (roundNeigh[0] > cord[0] && roundNeigh[1] == cord[1])
                            {
                                larger["equal"].Add(roundNeigh);
                            }

                            else if (roundNeigh[0] < cord[0] && roundNeigh[1] > cord[1])
                            {
                                smaller["larger"].Add(roundNeigh);
                            }

                            else if (roundNeigh[0] < cord[0] && roundNeigh[1] < cord[1])
                            {
                                smaller["smaller"].Add(roundNeigh);
                            }

                            else if (roundNeigh[0] < cord[0] && roundNeigh[1] == cord[1])
                            {
                                smaller["equal"].Add(roundNeigh);
                            }

                            else if (roundNeigh[0] == cord[0] && roundNeigh[1] > cord[1])
                            {
                                equal["larger"].Add(roundNeigh);
                            }

                            else if (roundNeigh[0] == cord[0] && roundNeigh[1] < cord[1])
                            {
                                equal["smaller"].Add(roundNeigh);
                            }
                        }
                    }
                }

                for (int m = 0; m < check.Count; m++)
                {
                    if (check[m] == true)
                    {
                        to_remove.Add(m);
                    }
                }

                to_remove.AddRange(CheckConditions(neighbours, larger["larger"], (a, b) => a >= b, (a, b) => a >= b, false, false));
                to_remove.AddRange(CheckConditions(neighbours, larger["smaller"], (a, b) => a >= b, (a, b) => a <= b, false, false));
                to_remove.AddRange(CheckConditions(neighbours, larger["equal"], (a, b) => a >= b, (a, b) => a >= b, true, false));

                to_remove.AddRange(CheckConditions(neighbours, smaller["larger"], (a, b) => a <= b, (a, b) => a >= b, false, false));
                to_remove.AddRange(CheckConditions(neighbours, smaller["smaller"], (a, b) => a <= b, (a, b) => a <= b, false, false));
                to_remove.AddRange(CheckConditions(neighbours, smaller["equal"], (a, b) => a <= b, (a, b) => a <= b, true, false));

                to_remove.AddRange(CheckConditions(neighbours, equal["larger"], (a, b) => a >= b, (a, b) => a >= b, true, true));
                to_remove.AddRange(CheckConditions(neighbours, equal["smaller"], (a, b) => a <= b, (a, b) => a <= b, true, true));

                to_remove = to_remove.Distinct().ToList();


                List<int[]> notVisited = new List<int[]>();
                for (int n = 0; n < neighbours.Count; n++)
                {
                    if (!to_remove.Contains(n))
                    {
                        notVisited.Add(neighbours[n]);
                    }
                }

                notVisited.Add(new int[] { x, y });

                // Update the seed value
                int updatedSeed = Seed + 1;
                // Create a new Random object with the updated seed
                random = new Random(updatedSeed);

                int[] cc = notVisited[random.Next(notVisited.Count)];
                Cell newC = Nested.Branch(cc[0])[cc[1]];

                //Draw Rectangle
                Curve rectangle;
                if (newC == Nested.Branch(cord[0])[cord[1]])
                {
                    List<Point3d> Verts = newC.GetVertices();
                    Plane plane = new Plane(Verts[0], Verts[1], Verts[2]);
                    Rectangle3d rect = new Rectangle3d(plane, Verts[0], Verts[2]);
                    rectangle = rect.ToNurbsCurve();
                }

                else
                {
                    List<double> dis0 = Distances(Nested.Branch(cord[0])[cord[1]].GetCenter(), newC.GetVertices());
                    int i0 = dis0.IndexOf(dis0.Max());
                    Point3d corner0 = newC.GetVertices()[i0];

                    List<double> dis1 = Distances(corner0, Nested.Branch(cord[0])[cord[1]].GetVertices());
                    int i1 = dis1.IndexOf(dis1.Max());
                    Point3d corner1 = Nested.Branch(cord[0])[cord[1]].GetVertices()[i1];

                    List<Point3d> Verts = Nested.Branch(cord[0])[cord[1]].GetVertices();
                    Plane plane = new Plane(Verts[0], Verts[1], Verts[2]);
                    Rectangle3d rect = new Rectangle3d(plane, corner0, corner1);
                    rectangle = rect.ToNurbsCurve();
                }

                //Mark as visited all cells inside rectangle
                List<int> vis_ind = new List<int>();
                //Create brep
                Brep Rbrep = Brep.CreatePlanarBreps(rectangle, 0.001)[0];

                foreach (int[] m in neighbours)
                {
                    List<Point3d> Vertis = Nested.Branch(m[0])[m[1]].GetVertices();
                    Plane plan = new Plane(Vertis[0], Vertis[1], Vertis[2]);
                    if (rectangle.Contains(Nested.Branch(m[0])[m[1]].GetCenter(), plan, 0.001) == PointContainment.Inside)
                    {
                        Visited.Branch(m[0])[m[1]] = true;
                        vis_ind.Add(m[0] * GridSize[1] + m[1]);
                    }
                }

                //Remove visited indexes from index list
                Counter = Counter.Where(t => !vis_ind.Contains(t)).ToList();

                //update seed
                Seed = 3 * Seed / 2;

                Rectangles.Add(rectangle);

            }


            Brep RB = Brep.CreatePlanarBreps(Rectangles[0], 0.001)[0];
            List<Point3d> Vv = Nested.Branch(3)[1].GetVertices();
            Plane pln = new Plane(Vv[0], Vv[1], Vv[2]);

            //Output
            //Output
            DA.SetDataList(0, Rectangles);

        }

        //Create class cell
        public class Cell
        {
            //Fields
            public Curve curve;

            //Constrcutor
            public Cell(Curve curve)
            {
                this.curve = curve;
            }

            //Methods
            //get center
            public Point3d GetCenter()
            {
                AreaMassProperties cntr = AreaMassProperties.Compute(this.curve, 0.001);
                return cntr.Centroid;
            }
            //get vertices
            public List<Point3d> GetVertices()
            {
                List<Point3d> vertices = new List<Point3d>();
                Polyline poly = null;
                if (this.curve.TryGetPolyline(out poly))
                {
                    vertices.AddRange(poly.ToArray());
                }
                return vertices;
            }
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
                var ImageBytes = Moth.Properties.Resources.RecursiveCellPartition;
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
            get { return new Guid("B2F4531B-2309-4A32-A444-31BD3BD4E7CB"); }
        }
    }
}