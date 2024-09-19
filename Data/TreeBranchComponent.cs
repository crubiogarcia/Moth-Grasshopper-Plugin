using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Rhino.DocObjects;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.ComponentModel;

namespace Moth
{
    public class TreeBranchComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public TreeBranchComponent()
          : base("Tree Branch Item", "TBranch",
              "Access branch data on a tree as a list item",
              "Moth", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Tree", "T", "Tree Input", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Index", "i", "Branch Index", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Branch", "Br", "Tree Branch", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Inputs

            // Define a GH_Structure to hold the data tree
            GH_Structure<IGH_Goo> T = new GH_Structure<IGH_Goo>();
            // Load the input tree using GetDataTree (specific for tree access)
            if (!DA.GetDataTree(0, out T))
            {
                return; // If data retrieval fails, exit early
            }
            //Index
            int idx = 0;
            DA.GetData(1, ref idx);

            //Error Handling
            if (idx < 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Index must be postive");
                return;
            }

            // Algorithm
            int n = idx % T.PathCount;
            Grasshopper.DataTree<object> branch = new Grasshopper.DataTree<object>();
            branch.AddRange(T.Branches[n], T.Paths[n]);

            //Output
            DA.SetDataTree(0, branch);

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
                var ImageBytes = Moth.Properties.Resources.TreeBranch;
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
            get { return new Guid("B57F30B0-5201-48FA-A4D7-BC8D516A1477"); }
        }
    }
}