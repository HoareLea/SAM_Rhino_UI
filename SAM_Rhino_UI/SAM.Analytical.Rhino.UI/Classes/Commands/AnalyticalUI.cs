// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Rhino;
using Rhino.Commands;

namespace SAM.Analytical.Rhino.UI
{
    public class AnalyticalUI : Command
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

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            string path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "SAM", "SAM Analytical.exe");
            if(!System.IO.File.Exists(path))
            {
                return Result.Failure;
            }

            Core.Query.StartProcess(path);
            return Result.Success;
        }
    }
}
