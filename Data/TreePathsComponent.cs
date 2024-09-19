using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Rhino.DocObjects;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Moth
{
    public class TreePathsComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public TreePathsComponent()
          : base("Tree Paths", "TPaths",
              "Gets the paths organized by branches",
              "Moth", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Tree", "T", "Tree Input", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Paths", "P", "Tree Paths", GH_ParamAccess.tree);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Define a GH_Structure to hold the data tree
            GH_Structure<IGH_Goo> T = null;
            // Load the input tree using GetDataTree (specific for tree access)
            if (!DA.GetDataTree(0, out T))
                return;

            //Algorithm
            // Initialize the result list
            Grasshopper.DataTree<int> paths = new Grasshopper.DataTree<int>();
            foreach (var p in T.Paths)
            {
                List<int> numbers = new List<int>();
                foreach (int num in p.Indices)
                {
                    numbers.Add(num);
                }

                paths.AddRange(numbers, p);
            }

            //Outputs
            DA.SetDataTree(0, paths);

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
                var ImageBytes = Moth.Properties.Resources.TreePaths;
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
            get { return new Guid("C7DAD406-5914-4BFC-AC17-179BED75AD66"); }
        }
    }
}