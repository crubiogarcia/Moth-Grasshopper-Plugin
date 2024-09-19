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
using System.Drawing.Imaging;
using System.Drawing;
using MetadataExtractor.Formats.Icc;


namespace Moth
{
    public class ImageColorProfileComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ImageColorProfileComponent class.
        /// </summary>
        public ImageColorProfileComponent()
          : base("Image Color Profile", "IColor",
              "Extract color profile tag from image metadata",
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
            pManager.AddTextParameter("Output", "O", "Output of trying to get Color info", GH_ParamAccess.item);
            pManager.AddTextParameter("Color space", "CA", "Image color space", GH_ParamAccess.item);
            pManager.AddTextParameter("Color profile", "CP", "Image color profile", GH_ParamAccess.item);
            pManager.AddTextParameter("Photometric Interpretation", "PI", "Image photometric interpretation", GH_ParamAccess.item);
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
            string colorspace = "Data not available";
            string colorprofile = "Data not available";
            string photometricinter = "Data not available";

            //Color profile
            var iccDirectory = directories.OfType<IccDirectory>().FirstOrDefault();
            if (iccDirectory != null)
            {
                // Use the correct tag for the profile version or class
                colorprofile = iccDirectory.GetDescription(IccDirectory.TagProfileVersion) ??
                               iccDirectory.GetDescription(IccDirectory.TagProfileClass) ??
                               "Unknown profile";
            }

            // Extract data from ExifIFD0Directory
            if (ExifIfd0Directory != null)
            {
                output = "Data available. Metadata extraction completed";
                // Extract the color space value from Exif data as a nullable int
                int? colorSpaceValue = null;
                //Try get color space
                try
                {
                    colorSpaceValue = ExifIfd0Directory.GetInt32(ExifDirectoryBase.TagColorSpace);
                }

                catch (MetadataExtractor.MetadataException)
                {
                    // If the tag does not exist, set the default value
                    colorspace = "Not available";
                }

                //Try get photometric interpretation
                var photometriDescription = ExifIfd0Directory.GetDescription(ExifDirectoryBase.TagPhotometricInterpretation);
                photometricinter = string.IsNullOrEmpty(photometriDescription) ? "Not available" : photometriDescription;


                // Map color space values to descriptions
                if (colorSpaceValue.HasValue)
                {
                    switch (colorSpaceValue.Value)
                    {
                        case 1:
                            colorspace = "sRGB";
                            break;
                        case 65535:
                            colorspace = "Uncalibrated";
                            break;
                        case 2:
                            colorspace = "Adobe RGB";
                            break;
                        case 3:
                            colorspace = "ProPhoto RGB";
                            break;
                        case 4:
                            colorspace = "CMYK";
                            break;
                        case 5:
                            colorspace = "Lab Color";
                            break;
                        default:
                            colorspace = "Unknown";
                            break;
                    }
                }

            }

            //List<string> test = new List<string>();
            
            //foreach (var tag in ExifIfd0Directory.Tags)
            //{
                //test.Add($"{tag.Name}: {tag.Description}");
            //}

            //Output
            DA.SetData(0, output);
            DA.SetData(1, colorspace);
            DA.SetData(2, colorprofile);
            DA.SetData(3, photometricinter);
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
                var ImageBytes = Moth.Properties.Resources.ImageColor;
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
            get { return new Guid("A0804B7D-BE4D-49F1-9D26-05AE7B1C4559"); }
        }
    }
}