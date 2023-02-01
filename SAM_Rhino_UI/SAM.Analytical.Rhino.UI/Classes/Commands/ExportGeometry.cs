using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.Rhino.UI
{
    public class ExportGeometry : Command
    {
        public ExportGeometry()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static ExportGeometry Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "SAM_ExportGeometry";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            List<RhinoObject> rhinoObjects = new List<RhinoObject>();

            RhinoObject[] rhinoObjects_Temp = doc.Objects.FindByObjectType(ObjectType.Brep);
            if(rhinoObjects_Temp != null)
            {
                rhinoObjects.AddRange(rhinoObjects_Temp);
            }

            List<ISAMGeometry3D> sAMGeometry3Ds = new List<ISAMGeometry3D>();
            foreach(RhinoObject rhinoObject in rhinoObjects)
            {
                List<ISAMGeometry3D> sAMGeometry3Ds_Temp = Geometry.Rhino.Convert.ToSAM(rhinoObject.Geometry as Brep);
                if(sAMGeometry3Ds_Temp == null || sAMGeometry3Ds_Temp.Count == 0)
                {
                    continue;
                }

                sAMGeometry3Ds.AddRange(sAMGeometry3Ds_Temp);
            }


            string path = null;
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    return Result.Cancel;
                }
                path = saveFileDialog.FileName;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return Result.Failure;
            }

            Result result = Core.Convert.ToFile(sAMGeometry3Ds, path) ? Result.Success : Result.Failure;

            return result;
        }
    }
}
