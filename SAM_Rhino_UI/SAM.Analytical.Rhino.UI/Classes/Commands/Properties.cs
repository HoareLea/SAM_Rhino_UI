// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Rhino.UI
{
    public class Properties : global::Rhino.Commands.Command
    {
        public Properties()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static Properties Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "SAM_Properties";

        protected override global::Rhino.Commands.Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            global::Rhino.Commands.Result result = RhinoGet.GetOneObject("Select AnalyticalObject", false, ObjectType.Brep | ObjectType.Point, out ObjRef objRef);
            if (result != global::Rhino.Commands.Result.Success || objRef == null)
            {
                return result;
            }

            if(!(objRef.Object() is RhinoObject rhinoObject))
            {
                return global::Rhino.Commands.Result.Failure;
            }

            switch(rhinoObject.ObjectType)
            {
                case ObjectType.Point:
                    return Run(objRef.Point());

                case ObjectType.Brep:
                    return Run(objRef.Brep());
            }

            return global::Rhino.Commands.Result.Failure;
        }

        private static global::Rhino.Commands.Result Run(Point point)
        {

            List<IAnalyticalObject> analyticalObjects = null;
            if (point.HasUserData)
            {
                string @string = point.GetUserString("SAM");
                if (!string.IsNullOrWhiteSpace(@string))
                {
                    analyticalObjects = Core.Convert.ToSAM<IAnalyticalObject>(@string);
                    analyticalObjects = analyticalObjects?.FindAll(x => x is Space);
                }
            }

            if (analyticalObjects == null || analyticalObjects.Count == 0)
            {
                Point3D point3D = Geometry.Rhino.Convert.ToSAM(point);
                if (point3D != null)
                {
                    analyticalObjects = new List<IAnalyticalObject>() { new Space($"{System.Guid.NewGuid().ToString()}", point3D) };
                }
            }

            List<Space> spaces = analyticalObjects.FindAll(x => x is Space)?.Cast<Space>()?.ToList();
            if (spaces != null)
            {
                Space space = null;

                // No AnalyticalModel is in scope here - this can be a Space just invented from the
                // clicked point (see above), not one read out of a real model. The WinForms SpaceForm
                // had a dedicated (space, enums) constructor for exactly this case, with no
                // ProfileLibrary/AdjacencyCluster; SpaceWindow degrades the same way with a null
                // AnalyticalModel (every AnalyticalModel-dependent path in InternalConditionControl is
                // guarded by a null-check before it would dereference it) - confirmed before writing
                // this, not assumed.
                Analytical.UI.WPF.SpaceWindow spaceWindow = new Analytical.UI.WPF.SpaceWindow(spaces?.First(), null, Core.Query.Enums(typeof(SpaceParameter)));

                new System.Windows.Interop.WindowInteropHelper(spaceWindow).Owner = global::Rhino.RhinoApp.MainWindowHandle();

                if (spaceWindow.ShowDialog() != true)
                {
                    return global::Rhino.Commands.Result.Cancel;
                }

                space = spaceWindow.Space;

                if (space != null)
                {
                    for (int i = 0; i < spaces.Count; i++)
                    {
                        spaces[i] = new Space(spaces[i].Guid, space, spaces[i].Name, spaces[i].Location);
                    }

                    string @string = Core.Convert.ToString(spaces);

                    point.SetUserString("SAM", @string);
                }

                return global::Rhino.Commands.Result.Success;
            }

            return global::Rhino.Commands.Result.Failure;
        }

        private static global::Rhino.Commands.Result Run(Brep brep)
        {
            List<IAnalyticalObject> analyticalObjects = null;
            if (brep.HasUserData)
            {
                string @string = brep.GetUserString("SAM");
                if (!string.IsNullOrWhiteSpace(@string))
                {
                    analyticalObjects = Core.Convert.ToSAM<IAnalyticalObject>(@string);
                    analyticalObjects = analyticalObjects?.FindAll(x => x is Panel || x is Aperture || x is ISpace);
                }
            }

            if (analyticalObjects == null || analyticalObjects.Count == 0)
            {
                List<ISAMGeometry3D> geometries = Geometry.Rhino.Convert.ToSAM(brep);
                if (geometries != null && geometries.Count != 0)
                {
                    analyticalObjects = new List<IAnalyticalObject>();
                    foreach (ISAMGeometry3D geometry in geometries)
                    {
                        List<Face3D> face3Ds = new List<Face3D>();

                        if (geometry is Face3D)
                        {
                            face3Ds.Add((Face3D)geometry);
                        }
                        else if (geometry is Shell)
                        {
                            face3Ds.AddRange(((Shell)geometry).Face3Ds);
                        }

                        if (face3Ds == null || face3Ds.Count == 0)
                        {
                            continue;
                        }

                        foreach (Face3D face3D in face3Ds)
                        {
                            if (face3D == null)
                            {
                                continue;
                            }

                            PanelType panelType = face3D.GetPlane().Normal.PanelType();

                            Panel panel_Temp = Create.Panel(Analytical.Query.DefaultConstruction(panelType), panelType, face3D);
                            if (panel_Temp == null)
                            {
                                continue;
                            }

                            analyticalObjects.Add(panel_Temp);
                        }

                    }
                }
            }

            if (analyticalObjects == null || analyticalObjects.Count == 0)
            {
                return global::Rhino.Commands.Result.Failure;
            }

            MaterialLibrary materialLibrary = Analytical.Query.DefaultMaterialLibrary();



            List<Panel> panels = analyticalObjects.FindAll(x => x is Panel)?.Cast<Panel>()?.ToList();
            if (panels != null && panels.Count != 0)
            {
                ConstructionLibrary constructionLibrary = Analytical.Query.DefaultConstructionLibrary();

                Panel panel = null;
                Analytical.UI.PanelWindow panelWindow = new Analytical.UI.PanelWindow(panels?.First(), materialLibrary, constructionLibrary, Core.Query.Enums(typeof(PanelParameter), typeof(Analytical.Solver.SolverParameter)));

                new System.Windows.Interop.WindowInteropHelper(panelWindow).Owner = global::Rhino.RhinoApp.MainWindowHandle();

                if (panelWindow.ShowDialog() != true)
                {
                    return global::Rhino.Commands.Result.Cancel;
                }

                panel = panelWindow.Panel;

                if (panel != null)
                {
                    for (int i = 0; i < panels.Count; i++)
                    {
                        panels[i] = Create.Panel(panels[i].Guid, panel, panels[i].PlanarBoundary3D);
                    }

                    string @string = Core.Convert.ToString(panels);

                    brep.SetUserString("SAM", @string);
                }

                return global::Rhino.Commands.Result.Success;
            }

            List<Aperture> apertures = analyticalObjects.FindAll(x => x is Aperture)?.Cast<Aperture>()?.ToList();
            if (apertures != null && apertures.Count != 0)
            {
                ApertureConstructionLibrary apertureConstructionLibrary = Analytical.Query.DefaultApertureConstructionLibrary();

                Aperture aperture = null;
                Analytical.UI.ApertureWindow apertureWindow = new Analytical.UI.ApertureWindow(apertures.FirstOrDefault(), materialLibrary, apertureConstructionLibrary, Core.Query.Enums(typeof(ApertureParameter)));

                new System.Windows.Interop.WindowInteropHelper(apertureWindow).Owner = global::Rhino.RhinoApp.MainWindowHandle();

                if (apertureWindow.ShowDialog() != true)
                {
                    return global::Rhino.Commands.Result.Cancel;
                }

                aperture = apertureWindow.Aperture;

                if (aperture != null)
                {
                    for (int i = 0; i < panels.Count; i++)
                    {
                        apertures[i] = new Aperture(apertures[i].Guid, aperture, apertures[i].Face3D);
                    }

                    string @string = Core.Convert.ToString(apertures);

                    brep.SetUserString("SAM", @string);
                }

                return global::Rhino.Commands.Result.Success;
            }

            return global::Rhino.Commands.Result.Failure;
        }
    }
}
