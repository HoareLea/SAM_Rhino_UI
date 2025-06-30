using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Rhino.UI
{
    public class AnalyticalUI : global::Rhino.Commands.Command
    {
        public AnalyticalUI()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static AnalyticalUI Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "SAM_AnalyticalUI";

        protected override global::Rhino.Commands.Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            string path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "SAM", "SAM Analytical.exe");
            if(!System.IO.File.Exists(path))
            {
                return global::Rhino.Commands.Result.Failure;
            }

            Core.Query.StartProcess(path);
            return global::Rhino.Commands.Result.Success;
        }
    }
}
