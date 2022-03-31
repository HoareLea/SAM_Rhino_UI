using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using SAM.Core;
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
            global::Rhino.Commands.Result result = RhinoGet.GetOneObject("Select Panel", false, ObjectType.Brep, out ObjRef objRef);
            if (result != global::Rhino.Commands.Result.Success || objRef == null)
            {
                return result;
            }

            Brep brep = objRef.Brep();
            if(brep == null)
            {
                return global::Rhino.Commands.Result.Failure;
            }

            List<Panel> panels = null;
            string @string = null;
            if (brep.HasUserData)
            {
                @string = brep.GetUserString("SAM");
                if (!string.IsNullOrWhiteSpace(@string))
                {
                    panels = Core.Convert.ToSAM<Panel>(@string);
                }
            }

            if(panels == null)
            {
                List<Geometry.Spatial.ISAMGeometry3D> geometries = Geometry.Rhino.Convert.ToSAM(brep);
                if (geometries != null && geometries.Count != 0)
                {
                    panels = new List<Panel>();
                    foreach (Geometry.Spatial.ISAMGeometry3D geometry in geometries)
                    {
                        List<Geometry.Spatial.Face3D> face3Ds = new List<Geometry.Spatial.Face3D>();

                        if (geometry is Geometry.Spatial.Face3D)
                        {
                            face3Ds.Add((Geometry.Spatial.Face3D)geometry);
                        }
                        else if (geometry is Geometry.Spatial.Shell)
                        {
                            face3Ds.AddRange(((Geometry.Spatial.Shell)geometry).Face3Ds);
                        }

                        if (face3Ds == null || face3Ds.Count == 0)
                        {
                            continue;
                        }

                        foreach (Geometry.Spatial.Face3D face3D in face3Ds)
                        {
                            if (face3D == null)
                            {
                                continue;
                            }

                            PanelType panelType = face3D.GetPlane().Normal.PanelType();

                            Panel panel_Temp = Create.Panel(Query.DefaultConstruction(panelType), panelType, face3D);
                            if (panel_Temp == null)
                            {
                                continue;
                            }

                            panels.Add(panel_Temp);
                        }

                    }
                }
            }


            MaterialLibrary materialLibrary = Query.DefaultMaterialLibrary();

            ConstructionLibrary constructionLibrary = Query.DefaultConstructionLibrary();

            Panel panel = null;
            using (Windows.Forms.PanelForm panelForm = new Windows.Forms.PanelForm(panels?.First(), materialLibrary, constructionLibrary, Core.Query.Enums(typeof(PanelParameter), typeof(Analytical.Solver.SolverParameter))))
            {
                if(panelForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return global::Rhino.Commands.Result.Cancel;
                }

                panel = panelForm.Panel;
            }

            if(panel != null)
            {
                for(int i =0; i < panels.Count; i++)
                {
                    panels[i] = Create.Panel(panels[i].Guid, panel, panels[i].PlanarBoundary3D);
                }

                @string = Core.Convert.ToString(panels);

                brep.SetUserString("SAM", @string);
            }

            return global::Rhino.Commands.Result.Success;
        }
    }
}
