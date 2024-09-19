using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Eto.Forms;
using System.Linq;
using Grasshopper.Getters;

namespace Moth
{
    public class MazeComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Maze class.
        /// </summary>
        public MazeComponent()
          : base("Maze", "Maze",
              "Creates a maze from a sorted grid input(tree of rows/columns)",
              "Moth", "Grid")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Sorted Grid", "G", "sorted grid input(tree of rows/columns)", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Seed", "S", "Random seed", GH_ParamAccess.item, 5);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Maze", "M", "Maze", GH_ParamAccess.list);
            pManager.AddCurveParameter("Paths", "P", "Paths", GH_ParamAccess.list);
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
            int S = 5;
            if (!DA.GetData(1, ref S)) return;

            //Functions
            // Returns true if end points of lines are similar under tolerance value
            List<Line> RemoveDuplicates(List<Line> lines, double tolerance)
            {
                // Copy original list
                List<Line> clean = new List<Line>(lines);

                // Clean up the list of duplicates
                for (int i = 0; i < clean.Count; i++)
                {
                    Line line = clean[i];

                    for (int j = clean.Count - 1; j > i; j--)
                    {
                        Line other = clean[j];

                        bool dup = AreDuplicate(line, other, tolerance);
                        if (dup == true)
                        {
                            clean.RemoveAt(j);
                        }
                    }
                }

                return clean;
            }

            //Checks if there are duplicate lines
            bool AreDuplicate(Line lineA, Line lineB, double tolerance)
            {
                // Compare starting points
                if (
                  (AreSimilar(lineA.From, lineB.From, tolerance) && AreSimilar(lineA.To, lineB.To, tolerance))
                  || (AreSimilar(lineA.From, lineB.To, tolerance) && AreSimilar(lineA.To, lineB.From, tolerance))
                )
                {
                    return true;
                }

                return false;
            }

            // Returns truu if coordinates of points are similar under a threshold value
            bool AreSimilar(Point3d a, Point3d b, double tol)
            {
                bool similar = Math.Abs(a.X - b.X) < tol
                  && Math.Abs(a.Y - b.Y) < tol
                  && Math.Abs(a.Z - b.Z) < tol;

                return similar;
            }


            List<int[]> GetNeighbours(int[] cell, List<List<Cell>> cells, int[] gridSize)
            {
                // Initiate empty Cell List
                List<int[]> neighbours = new List<int[]>();

                // List of offset values for neighbours (up, down, left, right)
                int[] offsets = { -1, 1 };

                foreach (int offset in offsets)
                {
                    // Check X neighbours
                    int x = cell[0] + offset;
                    // Check if x is inside the grid bounds
                    if (x >= 0 && x < gridSize[0] && !cells[x][cell[1]].isVisited)
                    {
                        neighbours.Add(new int[] { x, cell[1] });
                    }

                    // Check Y neighbours
                    int y = cell[1] + offset;
                    // Check if y is inside the grid bounds
                    if (y >= 0 && y < gridSize[1] && !cells[cell[0]][y].isVisited)
                    {
                        neighbours.Add(new int[] { cell[0], y });
                    }
                }

                return neighbours;

            }

            //Algorithm
            //Store the grid size
            GH_Path pth = new GH_Path(0);
            int[] GridSize = { G.PathCount, G[pth].Count };
            //Create list of cells
            List<List<Cell>> Cells = new List<List<Cell>>();

            //Create a list for the path
            List<Line> Path = new List<Line>();

            //Add items to Lists
            for (int i = 0; i < GridSize[0]; i++)
            {

                List<Cell> listCells = new List<Cell>();
                for (int j = 0; j < GridSize[1]; j++)
                {
                    // Get the GH_Curve and extract the Curve
                    GH_Path path = new GH_Path(i);
                    GH_Curve ghCurve = G[path][j] as GH_Curve;
                    if (ghCurve == null || ghCurve.Value == null) continue; // Handle null values
                    Curve Closecrv = ghCurve.Value;

                    AreaMassProperties cntr = AreaMassProperties.Compute(Closecrv, 0.001);
                    Point3d Center = cntr.Centroid;

                    //Iterate trough each row of the grid
                    Polyline Poly;

                    if (Closecrv.TryGetPolyline(out Poly))
                    {
                        List<Line> lines = Poly.GetSegments().ToList();
                        List<Curve> crvs = new List<Curve>();
                        foreach (Line ln in lines)
                        {
                            crvs.Add(ln.ToNurbsCurve());
                        }

                        Cell newCell = new Cell(crvs, Center);
                        listCells.Add(newCell);
                    }
                }
                Cells.Add(listCells);
            }
            //Create Stack
            Stack<int[]> stack = new Stack<int[]>();

            //Initiate random seed
            Random random = new Random(S);
            int ix = random.Next(GridSize[0] * GridSize[1]);

            // Translate index into the nested list indexes and Cell
            int idx = ix / GridSize[1];
            int idy = ix % GridSize[1];
            //Mark as visited
            Cells[idx][idy].MarkAsVisited();
            Cell VisitedCell = Cells[idx][idy];
            int[] VisitedCellIdx = { idx, idy };

            //Append to stack
            stack.Push(VisitedCellIdx);

            int testcounter = 29;

            while (stack.Count > 0)
            //while (testcounter > 0)
            {
                //Get Cell neighbours
                List<int[]> neighbours = GetNeighbours(VisitedCellIdx, Cells, GridSize);

                testcounter -= 1;

                if (neighbours.Count > 0)
                {
                    //Select Random neighbour
                    //Initiate random seed
                    Random randomseed = new Random(S);
                    int NeigIdx = randomseed.Next(neighbours.Count());
                    int[] Neig = neighbours[NeigIdx];
                    int[] Next = Neig;

                    stack.Push(Next);

                    //Mark VisitedCell as Visited
                    Cells[VisitedCellIdx[0]][VisitedCellIdx[1]].MarkAsVisited();

                    //Mark as visited
                    Cell NeigCell = Cells[Neig[0]][Neig[1]];

                    //Draw Line between centers of cells
                    Line LineCells = new Line(VisitedCell.Center, NeigCell.Center);

                    Path.Add(LineCells);

                    //Remove intersecting walls of the cells
                    Cells[Neig[0]][Neig[1]].DeleteWall(LineCells);
                    Cells[VisitedCellIdx[0]][VisitedCellIdx[1]].DeleteWall(LineCells);

                    //Set next
                    VisitedCellIdx = Neig;
                    VisitedCell = Cells[Neig[0]][Neig[1]];
                    S += 1;
                }

                else
                {
                    //Mark VisitedCell as Visited
                    Cells[VisitedCellIdx[0]][VisitedCellIdx[1]].MarkAsVisited();
                    //Continue
                    VisitedCellIdx = stack.Pop();
                    VisitedCell = Cells[VisitedCellIdx[0]][VisitedCellIdx[1]];
                    S += 1;
                }
            }

            //Get Curves from Cells
            List<Line> CellLines = new List<Line>();

            foreach (List<Cell> listcell in Cells)
            {
                foreach (Cell cl in listcell)
                {
                    foreach (Curve cellline in cl.Walls)
                    {
                        Line cln = new Line(cellline.PointAtEnd, cellline.PointAtStart);
                        CellLines.Add(cln);
                    }
                }
            }

            List<Line> M = RemoveDuplicates(CellLines, 0.001);

            //Output
            DA.SetDataList(0, M);
            DA.SetDataList(1, Path);

        }


        //Create class cell
        public class Cell
        {
            //Fields
            public List<Curve> Walls;
            public Point3d Center;
            public bool isVisited;

            //Constrcutor
            public Cell(List<Curve> Walls, Point3d Center)
            {
                this.Walls = Walls;
                this.isVisited = false;
                this.Center = Center;
            }

            //Methods
            //get vertices
            public List<Point3d> GetVertices()
            {
                Curve crv = Curve.JoinCurves(this.Walls)[0];
                List<Point3d> vertices = new List<Point3d>();
                Polyline poly = null;
                if (crv.TryGetPolyline(out poly))
                {
                    vertices.AddRange(poly.ToArray());
                }
                return vertices;
            }

            //Delete wall shared with neighbour
            public void DeleteWall(Line line)
            {
                // Collect walls to be removed
                List<Curve> wallsToRemove = new List<Curve>();
                foreach (Curve Wall in this.Walls)
                {
                    NurbsCurve curveline = line.ToNurbsCurve();
                    var intersect = Rhino.Geometry.Intersect.Intersection.CurveCurve(Wall, curveline, 0.001, 0.001);
                    // Check if there are any intersections
                    if (intersect != null && intersect.Count > 0)
                    {
                        wallsToRemove.Add(Wall);
                    }
                }
                // Remove collected walls
                foreach (Curve wall in wallsToRemove)
                {
                    this.Walls.Remove(wall);
                }
            }

            //Change Visited Status
            public void MarkAsVisited()
            {
                this.isVisited = true;
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
                var ImageBytes = Moth.Properties.Resources.Maze;
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
            get { return new Guid("5865C35E-8437-4FD3-A484-1B2E538B1E4E"); }
        }
    }
}