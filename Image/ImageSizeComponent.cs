using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Linq;
using System.Drawing;

namespace Moth
{
    public class ImageSizeComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ImageSizeComponent class.
        /// </summary>
        public ImageSizeComponent()
          : base("Image Size", "ISize",
              "Extract image size",
              "Moth", "Image")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("File path", "F", "Image file path", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "O", "Output", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width", "X", "Image width (pixels)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "Y", "Image height (pixels)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Input
            string F = null;
            if (!DA.GetData(0, ref F)) return;

            //Algorithm

            using (var image = Image.FromFile(F))
            {
                double width = image.Width;
                double height = image.Height;
                DA.SetData(0, "Image size found succesfully");
                DA.SetData(1, width);
                DA.SetData(2, height);
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
                var ImageBytes = Moth.Properties.Resources.ImageSize;
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
            get { return new Guid("B1FD314E-299A-4605-8E75-5C54482FB96C"); }
        }
    }
}