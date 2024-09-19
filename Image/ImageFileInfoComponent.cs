using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using static MetadataExtractor.Formats.Bmp.BmpHeaderDirectory;

namespace Moth
{
    public class ImageFileInfoComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ImageFileInfoComponent class.
        /// </summary>
        public ImageFileInfoComponent()
          : base("Image File Info", "IFileInfo",
              "Extract file info from image metadata",
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
            pManager.AddTextParameter("File size" , "S", "Size of the file in bytes", GH_ParamAccess.item);
            pManager.AddTextParameter("Compression", "C", "Compression details", GH_ParamAccess.item);
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

            //Default values
            string compression = "Data not available";
            string size = "Data not available";

            // Get file size using System.IO.FileInfo
            FileInfo fileInfo = new FileInfo(F);
            long fileSize = fileInfo.Length;

            size = fileSize.ToString() + " bytes";

            try
            { 
                // Extract metadata using MetadataExtractor
                var directories = ImageMetadataReader.ReadMetadata(F);

                // Extract compression info from JPEG or Exif directories
                var jpegDirectory = directories.OfType<JpegDirectory>().FirstOrDefault();
                if (jpegDirectory != null)
                {
                    compression = jpegDirectory.GetDescription(JpegDirectory.TagCompressionType) ?? "No compression data";
                }
            }

            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error reading metadata: " + ex.Message);
            }

            //Output
            DA.SetData(0, size);
            DA.SetData(1, compression);
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
                var ImageBytes = Moth.Properties.Resources.ImageFileInfo;
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
            get { return new Guid("594CC0EF-29F0-4FA2-8DE1-ED8AF4552048"); }
        }
    }
}