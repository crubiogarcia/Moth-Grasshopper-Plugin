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
    public class ImageCoordinatesComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ImageCoordinatesComponent class.
        /// </summary>
        public ImageCoordinatesComponent()
          : base("Image Coordinates", "ICoordinates",
              "Extract image GPS tag from its metadata",
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
            pManager.AddPointParameter("Point", "P", "Point of GPS tag from image", GH_ParamAccess.item);
            pManager.AddNumberParameter("Coordinates", "XYZ", " Latitude, Longitude and Altitude of th GPS tag where from image", GH_ParamAccess.list);
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

            var gps = ImageMetadataReader.ReadMetadata(F).OfType<GpsDirectory>().FirstOrDefault();

            if (gps != null)
            {
                var location = gps.GetGeoLocation();

                if (location != null)
                {
                    DA.SetData(0, "GPS metadata available.");

                    //Try get altitude
                    double altitude = gps.ContainsTag(GpsDirectory.TagAltitude)? gps.GetDouble(GpsDirectory.TagAltitude): 0.0; 

                    //Set list of coordinates
                    List<double> GPSList = new List<double> { location.Latitude, location.Longitude, altitude };
                    DA.SetDataList(2, GPSList);

                    //Set point of coordinates
                    Point3d P = new Point3d(location.Latitude, location.Longitude, altitude);
                    DA.SetData(1, P);
                    
                }
                else
                {
                    // Handle the case where GPS metadata exists but no location is found
                    DA.SetData(0, "GPS metadata found but no location data available.");
                }
            }

            else
            {
                // Handle the case where no GPS metadata is found
                DA.SetData(0, "GPS Tag not found");
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
                var ImageBytes = Moth.Properties.Resources.ImageGPSCoordinates;
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
            get { return new Guid("902403A4-036D-4A2B-8683-CF2C95AB3C54"); }
        }
    }
}