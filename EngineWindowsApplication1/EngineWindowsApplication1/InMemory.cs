using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesGDB;

namespace EngineWindowsApplication1
{
    class InMemory
    {
        public static IWorkspace CreateGDB(string WStype, string name, string path)
        {
            IWorkspace workspace = null;
            Type factoryType = Type.GetTypeFromProgID(WStype);
            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            if (workspaceFactory.IsWorkspace(path + name))
            {
                workspace = workspaceFactory.OpenFromFile(path + name, 0);
            }
            else
            {
                IWorkspaceName workspaceName = workspaceFactory.Create(path, name, null, 0);
                IName pName = (IName)workspaceName;
                workspace = (IWorkspace)pName.Open();
            }
            return workspace;
        }

        public static void CreateFCs(IWorkspace pWs, IMap pMap)
        {
            var FC1 = CreateFeatureClass(pWs, null, "Point", CreateFields(esriGeometryType.esriGeometryPoint));
            var FC2 = CreateFeatureClass(pWs, null, "Line", CreateFields(esriGeometryType.esriGeometryPolyline));
            var FC3 = CreateFeatureClass(pWs, null, "Polygon", CreateFields(esriGeometryType.esriGeometryPolygon));

            //insert& show
            List<IGeometry> ptList = new List<IGeometry>(100);
            IPoint pt;
            for (int i = 0; i < 1000; i++)
            {
                pt = new PointClass();
                pt.PutCoords(i * 0.01, i * 0.01);
                ptList.Add(pt);
            }

            List<IGeometry> lineList = new List<IGeometry>(100);
            IPolyline pLine;
            IPoint pt1, pt2;
            for (int i = 0; i < 1000; i++)
            {
                pLine = new PolylineClass();
                pt1 = new PointClass();
                pt1.PutCoords(i * 0.01, (i + 1) * 0.01);
                pt2 = new PointClass();
                pt2.PutCoords(i * 0.01, (i + 11) * 0.01);
                pLine.FromPoint = pt1;
                pLine.ToPoint = pt2;
                lineList.Add(pLine);
            }

            List<IGeometry> polygonList = new List<IGeometry>(100);
            IPolygon pPolygon;
            IPoint pt3, pt4;
            for (int i = 0; i < 1000; i++)
            {
                pPolygon = new PolygonClass();
                IPointCollection ptColl = pPolygon as IPointCollection;
                pt1 = new PointClass();
                pt1.PutCoords(i * 0.01, (i - 2) * 0.01);
                pt2 = new PointClass();
                pt2.PutCoords((i + 1) * 0.01, (i - 2) * 0.01);
                pt3 = new PointClass();
                pt3.PutCoords((i + 1) * 0.01, (i - 3) * 0.01);
                pt4 = new PointClass();
                pt4.PutCoords(i * 0.01, (i - 3) * 0.01);
                ptColl.AddPoint(pt1);
                ptColl.AddPoint(pt2);
                ptColl.AddPoint(pt3);
                ptColl.AddPoint(pt4);
                pPolygon.Close();
                polygonList.Add(pPolygon);
            }

            InsertFeaturesUsingCursor(FC1, ptList);
            InsertFeaturesUsingCursor(FC2, lineList);
            InsertFeaturesUsingCursor(FC3, polygonList);

            IFeatureLayer pFeaturelayer = new FeatureLayerClass();
            pFeaturelayer.Name = "pt";
            pFeaturelayer.FeatureClass = FC1;
            pMap.AddLayer(pFeaturelayer);
            pFeaturelayer = new FeatureLayerClass();
            pFeaturelayer.Name = "line";
            pFeaturelayer.FeatureClass = FC2;
            pMap.AddLayer(pFeaturelayer);
            pFeaturelayer = new FeatureLayerClass();
            pFeaturelayer.Name = "polygon";
            pFeaturelayer.FeatureClass = FC3;
            pMap.AddLayer(pFeaturelayer);
        }

        public static void InsertFeaturesUsingCursor(IFeatureClass featureClass, List<IGeometry> geometryList)
        {
            using (ComReleaser comReleaser = new ComReleaser())
            {
                IFeatureBuffer featureBuffer = featureClass.CreateFeatureBuffer();
                comReleaser.ManageLifetime(featureBuffer);

                IFeatureCursor insertCursor = featureClass.Insert(true);
                comReleaser.ManageLifetime(insertCursor);

                int typeFieldIndex = featureClass.FindField("Name");
                int i = 1;
                foreach (IGeometry geometry in geometryList)
                {
                    featureBuffer.Shape = geometry;
                    featureBuffer.set_Value(typeFieldIndex, i++.ToString());
                    insertCursor.InsertFeature(featureBuffer);
                }
                insertCursor.Flush();
            }
        }

        public static IFields CreateFields(esriGeometryType geoType)
        {
            IFeatureClassDescription fcDescription = new FeatureClassDescriptionClass();
            IObjectClassDescription ocDescription = (IObjectClassDescription)fcDescription;
            IFields fields = ocDescription.RequiredFields;

            int shapeFieldIndex = fields.FindField(fcDescription.ShapeFieldName);
            IField field = fields.get_Field(shapeFieldIndex);
            IGeometryDef geometryDef = field.GeometryDef;
            IGeometryDefEdit geometryDefEdit = (IGeometryDefEdit)geometryDef;
            geometryDefEdit.GeometryType_2 = geoType;
            ISpatialReferenceFactory spatialReferenceFactory = new SpatialReferenceEnvironmentClass();
            var sr = spatialReferenceFactory.CreateGeographicCoordinateSystem(4326);
            geometryDefEdit.SpatialReference_2 = sr;

            IFieldsEdit fieldsEdit = (IFieldsEdit)fields;
            field = new FieldClass();
            IFieldEdit fieldEdit = (IFieldEdit)field;
            fieldEdit.Name_2 = "Name";
            fieldEdit.Type_2 = esriFieldType.esriFieldTypeString;
            fieldsEdit.AddField(field);

            return fields;
        }

        public static IFeatureClass CreateFeatureClass(IWorkspace workspace, IFeatureDataset featureDataset, String featureClassName, IFields fields)
        {
            IFeatureClass featureClass;
            String strShapeField = "";
            var featureWorkspace = workspace as IFeatureWorkspace;
            IObjectClassDescription objectClassDescription = new FeatureClassDescriptionClass();

            #region 简单校验
            if (featureClassName == "") return null;
            //if (workspace.get_NameExists(esriDatasetType.esriDTFeatureClass, featureClassName))
            //{
            //    featureClass = featureWorkspace.OpenFeatureClass(featureClassName);
            //    return featureClass;
            //}

            if (fields == null)
            {
                fields = objectClassDescription.RequiredFields;
                IFieldsEdit fieldsEdit = (IFieldsEdit)fields;
                IField field = new FieldClass();
                IFieldEdit fieldEdit = (IFieldEdit)field;
                fieldEdit.Name_2 = "Name";
                fieldEdit.Type_2 = esriFieldType.esriFieldTypeString;
                fieldsEdit.AddField(field);
                fields = (IFields)fieldsEdit;
            }

            var feaClsDes = objectClassDescription as IFeatureClassDescription;
            strShapeField = feaClsDes.ShapeFieldName;
            #endregion

            //查询几何字段
            if (strShapeField == "")
            {
                for (int j = 0; j < fields.FieldCount; j++)
                {
                    if (fields.get_Field(j).Type == esriFieldType.esriFieldTypeGeometry)
                    {
                        strShapeField = fields.get_Field(j).Name;
                    }
                }
            }


            // 字段检查
            IFieldChecker fieldChecker = new FieldCheckerClass();
            IEnumFieldError enumFieldError = null;
            IFields validatedFields = null;
            fieldChecker.ValidateWorkspace = (IWorkspace)workspace;
            fieldChecker.Validate(fields, out enumFieldError, out validatedFields);

            //创建要素类
            if (featureDataset == null)
            {
                featureClass = featureWorkspace.CreateFeatureClass(featureClassName, validatedFields, objectClassDescription.InstanceCLSID, objectClassDescription.ClassExtensionCLSID, esriFeatureType.esriFTSimple, strShapeField, "");
            }
            else
            {
                featureClass = featureDataset.CreateFeatureClass(featureClassName, validatedFields, objectClassDescription.InstanceCLSID, objectClassDescription.ClassExtensionCLSID, esriFeatureType.esriFTSimple, strShapeField, "");
            }
            return featureClass;
        }
    }
}
