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
using Microsoft.SqlServer.Server;

namespace Moth
{
    public class FirstLastItemComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the FirstLastItemComponent class.
        /// </summary>
        public FirstLastItemComponent()
          : base("First Last Item", "FirstLast",
              "Extracts first and last items of a list",
              "Moth", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("List", "L", "List", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("First item", "First", "First item", GH_ParamAccess.item);
            pManager.AddGenericParameter("Last item", "Last", "last item", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Input
            List<Object> L = new List<Object>();
            DA.GetDataList(0, L);

            //Algorithm
            System.Object index0 = L[0];
            System.Object indexLast = L[L.Count - 1];

            //Output
            DA.SetData(0, index0);
            DA.SetData(1, indexLast);
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
                var ImageBytes = Moth.Properties.Resources.FirstLastItem;
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
            get { return new Guid("8685C7F7-F594-49A0-8255-47C2535231AB"); }
        }
    }
}