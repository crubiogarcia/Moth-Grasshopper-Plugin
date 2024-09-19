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

namespace Moth
{
    public class ImageCamaraComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ImageCamaraComponent class.
        /// </summary>
        public ImageCamaraComponent()
          : base("Image Camara", "ICamara",
              "Extract camera info tag from image metadata",
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
            pManager.AddTextParameter("Output", "O", "Output of trying to get GPS tag", GH_ParamAccess.item);
            pManager.AddTextParameter("Camera maker", "Cam", "Camera maker", GH_ParamAccess.item);
            pManager.AddTextParameter("Camera model", "Cmo", "Camera model", GH_ParamAccess.item);
            pManager.AddTextParameter("Aperture", "Ap", "Aperture", GH_ParamAccess.item);
            pManager.AddTextParameter("Exposure", "Ex", "Time of exposure", GH_ParamAccess.item);
            pManager.AddTextParameter("ISO", "ISO", "ISO Sensitivity", GH_ParamAccess.item);
            pManager.AddTextParameter("Focal Length", "f", "Focal Length", GH_ParamAccess.item);
            pManager.AddTextParameter("Flash mode", "Flash", "Flash mode", GH_ParamAccess.item);

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
            // Read metadata from the image file
            var directories = ImageMetadataReader.ReadMetadata(F);

            // Extract camera information from Exif directories
            var exifIFD0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var exifSubIFDDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

            // Default values
            string output = "Data not available";
            string cameraMaker = "Not available";
            string cameraModel = "Not available";
            string aperture = "Not available";
            string exposureTime = "Not available";
            string iso = "Not available";
            string focalLength = "Not available";
            string flashMode = "Not available";

            // Extract data from ExifIFD0Directory
            if (exifIFD0Directory != null)
            {
                cameraMaker = exifIFD0Directory.GetDescription(ExifIfd0Directory.TagMake) ?? "Not available";
                cameraModel = exifIFD0Directory.GetDescription(ExifIfd0Directory.TagModel) ?? "Not available";
                output = "Data available. Metadata extraction completed";
            }

            // Extract data from ExifSubIFDDirectory
            if (exifSubIFDDirectory != null)
            {
                aperture = exifSubIFDDirectory.GetDescription(ExifSubIfdDirectory.TagFNumber) ?? "Not available";
                exposureTime = exifSubIFDDirectory.GetDescription(ExifSubIfdDirectory.TagExposureTime) ?? "Not available";
                iso = exifSubIFDDirectory.GetDescription(ExifSubIfdDirectory.TagIsoEquivalent) ?? "Not available";
                focalLength = exifSubIFDDirectory.GetDescription(ExifSubIfdDirectory.TagFocalLength) ?? "Not available";
                flashMode = exifSubIFDDirectory.GetDescription(ExifSubIfdDirectory.TagFlash) ?? "Not available";
                output = "Data available. Metadata extraction completed";
            }

            //Output
            DA.SetData(0, output);
            DA.SetData(1, cameraMaker);
            DA.SetData(2, cameraModel);
            DA.SetData(3, aperture);
            DA.SetData(4, exposureTime);
            DA.SetData(5, iso);
            DA.SetData(6, focalLength);
            DA.SetData(7, flashMode);

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
                var ImageBytes = Moth.Properties.Resources.ImageCamara;
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
            get { return new Guid("9721B03F-7BE2-4578-B0AA-24B63D74EDCA"); }
        }
    }
}