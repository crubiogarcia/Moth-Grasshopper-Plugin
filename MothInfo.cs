using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Moth
{
    public class MothInfo : GH_AssemblyInfo
    {
        public override string Name => "Moth";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Moth is  a set on different functionalities that are useful in the Grasshopper Environment";

        public override Guid Id => new Guid("07bfbfdd-ba44-4898-9cd3-936650c2ae37");

        //Return a string identifying you or your company.
        public override string AuthorName => "Carmen Rubio Garcia";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "carmenrubio1@hotmail.es";

        //Version
        public override string Version => "1.0.1";
    }
}
