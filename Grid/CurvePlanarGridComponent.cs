using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Grasshopper.Kernel.Data;


namespace Moth
{
    public class CurvePlanarGridComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CurvePlanarGridComponent class.
        /// </summary>
        public CurvePlanarGridComponent()
          : base("CurvePlanarGridComponent", "CPlanarGrid",
              "Inscribes planar curve inside grid",
              "Moth", "Grid")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve", GH_ParamAccess.item);
            pManager.AddNumberParameter("Cell size", "S", "Cell size of grid", GH_ParamAccess.item, 3);
            pManager.AddPlaneParameter("Plane", "P", "Plane direction for grid", GH_ParamAccess.item, Plane.WorldXY); 
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Grid", "G", "Grid", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Inputs
            Curve C = null;
            if (!DA.GetData(0, ref C)) return;

            double S = 3;
            if (!DA.GetData(1, ref S)) return;

            Plane P = Plane.WorldXY;
            if (!DA.GetData(2, ref P)) return;

            //Error Handling: Check if Curves are planar
            if (C.IsPlanar() == false)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve C must be planar");
                return;
            }

            // Try to get the plane of the curve
            Plane curvePlane;
            if (!C.TryGetPlane(out curvePlane))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to fit a plane to the curve");
                return;
            }

            // Check if the curve's plane is the same as the given plane (within some tolerance)
            // Check if curve lies on the given plane
            double tolerance = Rhino.RhinoMath.ZeroTolerance;
            int numPointsToSample = 10; // Number of points to sample along the curve
            for (int i = 0; i < numPointsToSample; i++)
            {
                // Sample points along the curve (from 0.0 to 1.0 normalized parameter space)
                double t = i / (double)(numPointsToSample - 1);
                Point3d point = C.PointAtNormalizedLength(t);

                // Check the distance from the point to the plane
                double distanceToPlane = P.DistanceTo(point);

                if (distanceToPlane > tolerance)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve does not lie on the given plane");
                    return;
                }
            }

            //Functions
            //Bounding Box Edges
            List<Point3d> BoundingBoxEdges(Curve Crv, Plane Pl)
            {
                Box BB = new Box(Pl, Crv);
                //vertices
                Point3d[] verticesArray = BB.GetCorners();
                // Convert the array to a list
                List<Point3d> vertices = new List<Point3d>(verticesArray);
                //Get the 4 vertices of the box
                int startIndex = 5;
                if (vertices.Count > startIndex)
                {
                    vertices.RemoveRange(startIndex, vertices.Count - startIndex);
                }

                return vertices;
            }

            //GetCurve from Bounding Box
            Polyline GetPolyline(double miniX, double miniY, double maxiX, double maxiY, List<Point3d> points, Plane Pl)
            {
                List<Point3d> vertices = new List<Point3d>();
                //Get Diretcions
                Vector3d AxX = Pl.XAxis;
                Vector3d AxY = Pl.YAxis;
                Vector3d AxZ = Pl.YAxis;
                AxX.Unitize();
                AxY.Unitize();
                AxY.Unitize();

                //Generate the points
                double z1 = InterpolateZ(miniX, miniY, points, Pl);
                double z2 = InterpolateZ(maxiX, miniY, points, Pl);
                double z3 = InterpolateZ(maxiX, maxiY, points, Pl);
                double z4 = InterpolateZ(miniX, maxiY, points, Pl);

                Vector3d combinedVector1 = AxX * miniX + AxY * miniY + AxZ * z1;
                Point3d Point1 = new Point3d(combinedVector1);
                Vector3d combinedVector2 = AxX * maxiX + AxY * miniY + AxZ * z2;
                Point3d Point2 = new Point3d(combinedVector2);
                Vector3d combinedVector3 = AxX * maxiX + AxY * maxiY + AxZ * z3;
                Point3d Point3 = new Point3d(combinedVector3);
                Vector3d combinedVector4 = AxX * miniX + AxY * maxiY + AxZ * z4;
                Point3d Point4 = new Point3d(combinedVector4);

                // Add points to the list
                vertices.Add(Point1);
                vertices.Add(Point2);
                vertices.Add(Point3);
                vertices.Add(Point4);
                vertices.Add(Point1);

                //Polyline
                Polyline Polyln = new Polyline(vertices);

                return Polyln;
            }
            
            //Get Macx and Min Values of coordinates in the plane system
            List<double> GetMaxMinValues(List<Point3d> Points, Plane Pl)
            {
                //Get Diretcions
                Vector3d AxX = Pl.XAxis;
                Vector3d AxY = Pl.YAxis;
                Vector3d AxZ = Pl.ZAxis;
                AxX.Unitize();
                AxY.Unitize();
                AxZ.Unitize();
                //Initialize empty list
                List<double> MaxMinValues = new List<double>();

                // Initialize min and max dot product values
                double minDotX = double.MaxValue;
                double maxDotX = double.MinValue;
                double minDotY = double.MaxValue;
                double maxDotY = double.MinValue;
                double minDotZ = double.MaxValue;
                double maxDotZ = double.MinValue;

                // Iterate through the list of points
                foreach (Point3d point in Points)
                {
                    double dotX = point.X * AxX.X + point.Y * AxX.Y + point.Z * AxX.Z;
                    double dotY = point.X * AxY.X + point.Y * AxY.Y + point.Z * AxY.Z;
                    double dotZ = point.X * AxZ.X + point.Y * AxZ.Y + point.Z * AxZ.Z;

                    if (dotX < minDotX) minDotX = dotX;
                    if (dotX > maxDotX) maxDotX = dotX;

                    if (dotY < minDotY) minDotY = dotY;
                    if (dotY > maxDotY) maxDotY = dotY;

                    if (dotZ < minDotZ) minDotZ = dotZ;
                    if (dotZ > maxDotZ) maxDotZ = dotZ;
                }

                MaxMinValues.Add(minDotX);
                MaxMinValues.Add(maxDotX);
                MaxMinValues.Add(minDotY);
                MaxMinValues.Add(maxDotY);
                MaxMinValues.Add(minDotZ);
                MaxMinValues.Add(maxDotZ);

                return MaxMinValues;

            }
            
            
            //Interpolate Z
            double InterpolateZ(double x, double y, List<Point3d> points, Plane Pl)
            {
                Point3d p1 = Point3d.Unset, p2 = Point3d.Unset, p3 = Point3d.Unset;
                double d1 = double.MaxValue, d2 = double.MaxValue, d3 = double.MaxValue;

                foreach (var point in points)
                {
                    double distance = (point.X - x) * (point.X - x) + (point.Y - y) * (point.Y - y);
                    if (distance < d1)
                    {
                        d3 = d2;
                        p3 = p2;
                        d2 = d1;
                        p2 = p1;
                        d1 = distance;
                        p1 = point;
                    }
                    else if (distance < d2)
                    {
                        d3 = d2;
                        p3 = p2;
                        d2 = distance;
                        p2 = point;
                    }
                    else if (distance < d3)
                    {
                        d3 = distance;
                        p3 = point;
                    }
                }

                double totalDistance = Math.Sqrt(d1) + Math.Sqrt(d2) + Math.Sqrt(d3);
                double z = (p1.Z * (Math.Sqrt(d1) / totalDistance)) + (p2.Z * (Math.Sqrt(d2) / totalDistance)) + (p3.Z * (Math.Sqrt(d3) / totalDistance));

                return z;
            }

            //Algorithm
            //Polyline Box
            List<Point3d> verts = BoundingBoxEdges(C, P);

            //Get Diretcions
            Vector3d AxisX = P.XAxis;
            Vector3d AxisY = P.YAxis;
            AxisX.Unitize();
            AxisY.Unitize();

            //Get dimensions
            List<double> Values = GetMaxMinValues(verts, P);
            double maxX = Values[1];
            double minX = Values[0];
            double maxY = Values[3];
            double minY = Values[2];
            double maxZ = Values[5];
            double minZ = Values[4];
            double Z = verts[0].Z;

            //Calculate Bounding Box Dimensions
            double width = maxX - minX;
            double depth = maxY - minY;

            // Step 4: Calculate Number of Cells
            int numCellsX = (int)Math.Ceiling(width / S);
            int numCellsY = (int)Math.Ceiling(depth / S);

            //Create Grid
            List<List<Polyline>> grid = new List<List<Polyline>>(numCellsY);

            // Step 6: Populate Grid with Cells
            for (int i = 0; i < numCellsY; i++)
            {
                List<Polyline> row = new List<Polyline>(numCellsX);
                for (int j = 0; j < numCellsX; j++)
                {
                    double cellMinX = minX + j * S;
                    double cellMinY = minY + i * S;
                    double cellMaxX = cellMinX + S;
                    double cellMaxY = cellMinY + S;

                    Polyline poly = GetPolyline(cellMinX, cellMinY, cellMaxX, cellMaxY, verts, P);
                    row.Add(poly);
                }
                grid.Add(row);
            }


            DataTree<object> GridTree = new DataTree<object>();
            for (int i = 0; i < grid.Count; i++)
            {
                for (int j = 0; j < grid[i].Count; j++)
                {
                    GridTree.Add(grid[i][j], new GH_Path(i));
                }
            }

            //Ouput
            DA.SetDataTree(0, GridTree);

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
                var ImageBytes = Moth.Properties.Resources.CurvePlanarGrid;
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
            get { return new Guid("8D122583-368C-41DA-9F96-64C3675F7642"); }
        }
    }
}