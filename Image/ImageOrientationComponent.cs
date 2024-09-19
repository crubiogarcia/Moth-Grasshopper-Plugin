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
using Eto.Forms;


namespace Moth
{
    public class ImageOrientationComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ImageOrientationComponent class.
        /// </summary>
        public ImageOrientationComponent()
          : base("Image Orientation", "I Orientation",
              "Extract Orientation tag from image metadata",
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
            pManager.AddTextParameter("Output", "O", "Output of trying to get Orientation tag", GH_ParamAccess.item);
            pManager.AddTextParameter("Orientation", "Or", "Image Orientation", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Angle", "A", "Angle of rotation", GH_ParamAccess.item);
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
            var ExifIfd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

            // Default value
            string output = "Data not available";
            string orientation = "Data not available";
            int? angle = null;
            int? orientationTag = null;

            // Extract data from ExifIFD0Directory
            if (ExifIfd0Directory != null)
            {
 
                output = "Data available. Metadata extraction completed";
                // Extract the orientation information
                orientationTag = ExifIfd0Directory.GetInt32(ExifDirectoryBase.TagOrientation);

            }

            if (orientationTag != null)
            {
                // Read the orientation tag and set the orientation and angle
                switch (orientationTag)
                {
                    case 1:
                        orientation = "Normal";
                        angle = 0;
                        break;
                    case 3:
                        orientation = "Upside-down";
                        angle = 180;
                        break;
                    case 6:
                        orientation = "Rotated 90° CW";
                        angle = 90;
                        break;
                    case 8:
                        orientation = "Rotated 90° CCW";
                        angle = 270;
                        break;
                    default:
                        orientation = "Unknown orientation";
                        //angle = 0;
                        break;
                }
            }

            //Output
            DA.SetData(0, output);
            DA.SetData(1, orientation);
            DA.SetData(2, angle);

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
                var ImageBytes = Moth.Properties.Resources.ImageOrientation;
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
            get { return new Guid("52024F5C-DBF5-41E8-9834-BAC4EBA4161F"); }
        }
    }
}