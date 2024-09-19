using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Drawing;
using Rhino;
using Rhino.Geometry;
using Grasshopper;

namespace Moth.Utils
{
    public class TabProperties : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            var server = Grasshopper.Instances.ComponentServer;

            server.AddCategoryShortName("Moth", "M");
            server.AddCategorySymbolName("Moth", 'M');

            // Assuming Properties.Resources.MothIcon is a byte[]
            byte[] mothIconBytes = Properties.Resources.MothIcon;

            // Convert byte[] to Bitmap
            using (MemoryStream ms = new MemoryStream(mothIconBytes))
            {
                Bitmap mothIcon = new Bitmap(ms);
                server.AddCategoryIcon("Moth", mothIcon);
            }

            return GH_LoadingInstruction.Proceed;

        }


    }
}
