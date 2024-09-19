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
    public class ImageDateTimeComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ImageDateTime class.
        /// </summary>
        public ImageDateTimeComponent()
          : base("Image Date Time", "IDateTime",
              "Extract Date Time tag from image metadata",
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
            pManager.AddTextParameter("Output", "O", "Output of trying to get Date Time tag", GH_ParamAccess.item);
            pManager.AddTextParameter("Date", "D", "Date metadata", GH_ParamAccess.item);
            pManager.AddTextParameter("Time", "T", "Time metadata", GH_ParamAccess.item);
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
            string date = "Data not available";
            string time = "Data not available";
            string datetime = "Empty";

            // Extract data from ExifIFD0Directory
            if (ExifIfd0Directory != null)
            {
                datetime = ExifIfd0Directory.GetDescription(ExifDirectoryBase.TagDateTime) ?? "Not available";
                output = "Data available. Metadata extraction completed";
            }

            // Try to get split the datetime string (format: "YYYY:MM:DD HH:MM:SS")
            if (datetime != "Empty")
            {
                // Split the datetime into date and time parts
                var datetimeParts = datetime.Split(' '); // Split the datetime string by the space

                if (datetimeParts.Length == 2)
                {
                    date = datetimeParts[0];  // Extract the date part (YYYY:MM:DD)
                    time = datetimeParts[1];  // Extract the time part (HH:MM:SS)
                }
            }


            //Output
            DA.SetData(0, output);
            DA.SetData(1, date);
            DA.SetData(2, time);

        }

        //Set exposure level
        public override GH_Exposure Exposure => base.Exposure;

        /// <summary>
        /// Provides an Icon for the component.S
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var ImageBytes = Moth.Properties.Resources.ImageDateTime;
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
            get { return new Guid("01DBBCA2-2AD3-433E-BE76-4DF4319EAB6E"); }
        }
    }
}