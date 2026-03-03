// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using SAM.Geometry.Rhino;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Rhino.UI
{
    public class Import : Command
    {
        public Import()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static Import Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "SAM_ImportJSON";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            List<IAnalyticalObject> analyticalObjects = Windows.Query.Import<IAnalyticalObject>(x => x is Panel || x is Aperture || x is ISpace, new Windows.ImportOptions());
            if(analyticalObjects is null || analyticalObjects.Count == 0)
            {
                return Result.Cancel;
            }

            LayerTable layerTable = doc?.Layers;
            if (layerTable == null)
            {
                return Result.Failure;
            }

            Layer layer_SAM = Core.Rhino.Modify.AddSAMLayer(layerTable);
            if (layer_SAM == null)
            {
                return Result.Failure;
            }

            int index = 0;

            Layer layer_Spaces = null;
            Layer layer_ApertureType = null;
            Layer layer_PanelType = null;

            if(analyticalObjects.Any(x => x is Aperture))
            {
                index = layerTable.Add();
                layer_ApertureType = layerTable[index];
                layer_ApertureType.Name = "ApertureType";
                layer_ApertureType.ParentLayerId = layer_SAM.Id;
            }

            if (analyticalObjects.Any(x => x is Panel))
            {
                index = layerTable.Add();
                layer_PanelType = layerTable[index];
                layer_PanelType.Name = "PanelType";
                layer_PanelType.ParentLayerId = layer_SAM.Id;
            }

            if (analyticalObjects.Any(x => x is Space))
            {
                index = layerTable.Add();
                layer_Spaces = layerTable[index];
                layer_Spaces.Name = "Spaces";
                layer_Spaces.ParentLayerId = layer_SAM.Id;
            }

            ObjectAttributes objectAttributes = doc.CreateDefaultAttributes();

            foreach (IAnalyticalObject analyticalObject in analyticalObjects)
            {
                System.Guid guid = System.Guid.Empty;

                if (analyticalObject is Panel panel)
                {
                    Layer layer = Core.Rhino.Modify.GetLayer(layerTable, layer_PanelType.Id, panel.GetType().Name, Query.Color(panel));
                    objectAttributes.LayerIndex = layer.Index;

                    guid = doc.Objects.AddBrep(panel.Face3D.ToRhino_Brep(), objectAttributes);
                }
                else if(analyticalObject is Aperture aperture)
                {
                    ApertureType apertureType = aperture.ApertureType;

                    Layer layer = Core.Rhino.Modify.GetLayer(layerTable, layer_ApertureType.Id, apertureType.ToString(), Query.Color(apertureType));
                    objectAttributes.LayerIndex = layer.Index;

                    guid = doc.Objects.AddBrep(aperture.Face3D.ToRhino_Brep(), objectAttributes);
                }
                else if(analyticalObject is Space space)
                {
                    string internalConditionName = space.InternalCondition?.Name;
                    if (string.IsNullOrWhiteSpace(internalConditionName))
                        internalConditionName = "???";

                    System.Drawing.Color color = Core.Create.Color(internalConditionName);

                    Layer layer_Level = Core.Rhino.Modify.GetLayer(layerTable, layer_Spaces.Id, internalConditionName, color);

                    string layerName = space.Name;
                    if (string.IsNullOrWhiteSpace(layerName))
                        layerName = "???";

                    color = Core.Create.Color(layerName);

                    Layer layer_Space = Core.Rhino.Modify.GetLayer(layerTable, layer_Level.Id, layerName, color);

                    objectAttributes.LayerIndex = layer_Space.Index;

                    guid = doc.Objects.AddPoint(space.Location.ToRhino(), objectAttributes);
                }

                if(guid == System.Guid.Empty)
                {
                    continue;
                }

                GeometryBase geometryBase = doc.Objects.FindGeometry(guid);
                if (geometryBase is null)
                {
                    continue; 
                }


                string @string = Core.Convert.ToString(analyticalObject);
                geometryBase.SetUserString("SAM", @string);
            }

            return Result.Success;
        }
    }
}
