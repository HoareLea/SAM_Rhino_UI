// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Rhino.UI
{
    public class Solve : Command
    {
        public Solve()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static Solve Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "SAM_Solve";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            IEnumerable<RhinoObject> rhinoObjects = doc.Objects.GetSelectedObjects(false, false);
            if(rhinoObjects is null || rhinoObjects.Count() == 0)
            {
                return Result.Failure;
            }

            List<IAnalyticalObject> analyticalObjects = rhinoObjects.ToSAM();
            if (analyticalObjects is null || analyticalObjects.Count == 0)
            {
                return Result.Failure;
            }

            AnalyticalModel analyticalModel = new AnalyticalModel(System.Guid.NewGuid(), "Solver");

            List<Aperture> apertures = new List<Aperture>();

            foreach (IAnalyticalObject analyticalObject in analyticalObjects)
            {
                if(analyticalObject is Panel panel)
                {
                    analyticalModel.AddPanel(panel);
                }
                else if (analyticalObject is Space space)
                {
                    analyticalModel.AddSpace(space);
                }
                else if(analyticalObject is Aperture aperture)
                {
                    apertures.Add(aperture);
                }
            }

            analyticalModel = Analytical.UI.WPF.Modify.Solve(analyticalModel, out _);
            if(analyticalModel is null)
            {
                return Result.Failure; 
            }

            if(apertures != null && apertures.Count != 0)
            {
                AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
                if(adjacencyCluster != null)
                {
                    apertures = adjacencyCluster.AddApertures(apertures);
                }

                if(apertures != null && apertures.Count != 0)
                {
                    analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);
                }

            }

            string directory = Core.Query.UserSAMTemporaryDirectory();
            if(string.IsNullOrWhiteSpace(directory))
            {
                return Result.Failure; 
            }

            if(!System.IO.Directory.Exists(directory))
            {
                if(!Core.Create.Directory(directory))
                {
                    return Result.Failure; 
                }
            }

            string path = System.IO.Path.Combine(directory, "solver.json");

            if(!Core.Convert.ToFile(analyticalModel, path))
            {
                return Result.Failure;
            }

            Core.StartupOptions startupOptions = new Core.StartupOptions()
            {
                Path = path,
                TemporaryFile = true,
            };

            string path_Application = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "SAM", "SAM Analytical.exe");
            if (!System.IO.File.Exists(path_Application))
            {
                return Result.Failure;
            }

            Core.Query.StartProcess(path_Application, startupOptions.ToString());

            return Result.Success;
        }
    }
}
