using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Rhino;

namespace Moth
{
    public class RecursiveSnowflakeComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the RecursiveSnowflakeComponent class.
        /// </summary>
        public RecursiveSnowflakeComponent()
          : base("RecursiveSnowflakeComponent", "RSnowflake",
              "Snowflake through recursion",
              "Moth", "Curve")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "P", "Center point", GH_ParamAccess.item, new Point3d(0, 0, 0));
            pManager.AddNumberParameter("Radius", "R", "Radius of snowflake", GH_ParamAccess.item, 5.0);
            pManager.AddIntegerParameter("Branches", "B", "Number of branches", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Width", "W", "Width of branches", GH_ParamAccess.item, 0.3);
            pManager.AddNumberParameter("Depth", "D", "Depth of branches", GH_ParamAccess.item,0.6);
            pManager.AddIntegerParameter("Iterations", "i", "Number of iterations", GH_ParamAccess.item, 3);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Snowflake", "S", "Recursive snowflake", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Input
            Point3d P = new Point3d(0,0,0);
            if (!DA.GetData(0, ref P)) return;
            double R = 5.0;
            if (!DA.GetData(1, ref R)) return;
            int B = 4;
            if (!DA.GetData(2, ref B)) return;
            double W = 0.3;
            if (!DA.GetData(3, ref W)) return;
            double D = 0.6;
            if (!DA.GetData(4, ref D)) return;
            int i = 3;
            if (!DA.GetData(5, ref i)) return;

            //Error Handling
            if (B <1)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "B must be postive");
                return;
            }

            //Functions

            //Snowflake
            List<Line> Snowflake(List<Line> Lins, double Depth, double Width)
            {
                //Start empty list
                List<Line> Snowf = new List<Line>();

                //Iterate through the list of lines
                foreach (Line ln in Lins)
                {
                    //Start and End points
                    Point3d Start = ln.From;
                    Point3d End = ln.To;
                    //Length of line
                    double Length = ln.Length;
                    //Midpoint
                    Point3d P0 = ln.PointAtLength(Length / 2);
                    //Normal Vector
                    double dX = End.X - Start.X;
                    double dY = End.Y - Start.Y;
                    double dZ = End.Z - Start.Z;

                    Vector3d Vector = new Vector3d(dY, -dX, -dZ);
                    Vector.Unitize();

                    //Points
                    Point3d P1 = ln.PointAtLength(Length * Depth);
                    Point3d P2 = P1;
                    //Transformations
                    Transform transform1 = Transform.Translation(Vector * Length * Width);
                    Transform transform2 = Transform.Translation(-Vector * Length * Width);

                    P1.Transform(transform1);
                    P2.Transform(transform2);

                    //New Lines
                    Line newLine1 = new Line(Start, P0);
                    Line newLine2 = new Line(P0, End);
                    Line newLine3 = new Line(P0, P1);
                    Line newLine4 = new Line(P0, P2);

                    Snowf.Add(newLine1);
                    Snowf.Add(newLine2);
                    Snowf.Add(newLine3);
                    Snowf.Add(newLine4);
                }

                return Snowf;
            }

            //Polar Array
            List<Line> Polar(Line lin, Point3d center, int rotations)
            {
                //Start empty list
                List<Line> lines = new List<Line>();
                //Angle
                double angleStep = 360 / rotations;
                //Rotate Line
                for (int j = 0; j < rotations; j++)
                {
                    double angle = angleStep * j;
                    Line newLine = lin;
                    Transform transform = Transform.Rotation(RhinoMath.ToRadians(angle), center);
                    newLine.Transform(transform);
                    lines.Add(newLine);
                }

                //Return line
                return lines;
            }

            //Recursion
            List<Line> recursive(List<Line> Lns, int iterations, List<Line> List, double Depth, double Width)
            {
                //Run recursion
                if (iterations > 0)
                {
                    List<Line> NewLines = Snowflake(Lns, Depth, Width);
                    iterations -= 1;
                    recursive(NewLines, iterations, List, Depth, Width);

                    if (iterations == 0)
                    {
                        List.AddRange(NewLines);
                    }
                }

                //Return result
                return List;
            }

            //Algorithm

            //Create base Line
            Vector3d vector = new Vector3d(0, R, 0);
            Point3d Pt2 = new Point3d(P + vector);
            //Empty line
            List<Line> Lines = new List<Line>();
            Line line = new Line(P, Pt2);
            Lines.Add(line);

            //Rotate
            List<Line> ArrayPolar = Polar(line, P, B);

            //Empty Line
            List<Line> ListEmpty = new List<Line>();

            //Run Recursion
            List<Line> SFlake = recursive(ArrayPolar, i, ListEmpty, D, W);

            //Output
            DA.SetDataList(0, SFlake);
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
                var ImageBytes = Moth.Properties.Resources.RecursiveSnowflake;
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
            get { return new Guid("E4B68181-107E-43F5-A221-75210C3E541C"); }
        }
    }
}