using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.GeoAnalyst;

namespace EngineWindowsApplication1
{
    public partial class Form1 : Form
    {
        IMapControl2 mapControl;
        string currentDir = System.IO.Directory.GetCurrentDirectory() + "\\Data";
        string pointFile = "points.shp";
        string lineFile = "lines.shp";
        string polygonFile = "polygons.shp";
        string bouGDB = "ClipBorders.gdb";
        ISpatialReference spaRef;

        public static int groupNumber = 0;
        public static int extentStatus1;
        public static int extentStatus2;
        public static int extentStatus3;
        List<IGroupLayer> avlbLayers = new List<IGroupLayer>();

        public Form1()
        {
            InitializeComponent();
            mapControl = axMapControl1.Object as IMapControl2;
            
            for (int count = 1; count <= listBox1.Items.Count; count++)
            {
                string path = currentDir + "\\" + count.ToString();
                LoadOriginData(path, count);           
            }
        }

        public void LoadOriginData(string path, int groupNumber)
        {
            IWorkspaceFactory shpFactory = new ShapefileWorkspaceFactoryClass();
            IFeatureWorkspace shpFeaWksp = shpFactory.OpenFromFile(path, 0) as IFeatureWorkspace;
            IFeatureClass fcPoint = shpFeaWksp.OpenFeatureClass(pointFile);
            IFeatureClass fcLine = shpFeaWksp.OpenFeatureClass(lineFile);
            IFeatureClass fcPlygon = shpFeaWksp.OpenFeatureClass(polygonFile);

            IFeatureLayer lyPoint = new FeatureLayerClass();
            lyPoint.FeatureClass = fcPoint;
            IFeatureLayer lyLine = new FeatureLayerClass();
            lyLine.FeatureClass = fcLine;
            IFeatureLayer lyPlygon = new FeatureLayerClass();
            lyPlygon.FeatureClass = fcPlygon;

            IGroupLayer grpLayer = new GroupLayerClass();
            grpLayer.Name = "grp" + groupNumber.ToString() + "ext0";
            grpLayer.Add(lyPoint);
            grpLayer.Add(lyLine);
            grpLayer.Add(lyPlygon);

            avlbLayers.Add(grpLayer);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Clear();

            groupNumber = listBox1.SelectedIndex + 1;
            int extentStatus = findExtentStatus(groupNumber);
            comboBox1.SelectedIndex = extentStatus;

            bool exist = CheckAvailability(groupNumber, extentStatus);
            string findName = "grp" + groupNumber.ToString() + "ext" + extentStatus.ToString();

            if (exist == true)
            {
                Clear();
                IGroupLayer viewGrpLys = avlbLayers.Find(item => item.Name == findName);
                mapControl.AddLayer(viewGrpLys);
            }
            else
            {
                Clear();
                ClipByExtent(groupNumber, extentStatus);
                IGroupLayer newGrpLys = avlbLayers.Find(item => item.Name == findName);
                mapControl.AddLayer(newGrpLys);
            }

        }

        public void ClipByExtent(int groupNm, int extNum)
        {
            IGeometry extent = GetClipExtent(extNum);
            string findName = "grp" + groupNm.ToString() + "ext0";
            IGroupLayer grpLy = avlbLayers.Find(item => item.Name == findName);
            ICompositeLayer comLy = grpLy as ICompositeLayer;

            IGroupLayer newGroupLayer = new GroupLayerClass();
            newGroupLayer.Name = "grp" + groupNm.ToString() + "ext" + extNum.ToString();

            for (int i = 0; i < comLy.Count; i++)
            {
                IFeatureLayer feaLy = comLy.get_Layer(i) as IFeatureLayer;
                IFeatureClass clipFC = feaLy.FeatureClass;
                IFields flds = clipFC.Fields;

                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = extent;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                IFeatureCursor cursor = clipFC.Search(spatialFilter, false);

                IWorkspaceFactory workspaceFactory = new InMemoryWorkspaceFactoryClass();
                IWorkspaceName workspaceName = workspaceFactory.Create(null, "MyWorkspace", null, 0);
                IName name = (IName)workspaceName;
                IWorkspace workspace = (IWorkspace)name.Open();
                IFeatureWorkspace inmemFeaWksp = workspace as IFeatureWorkspace;

                if (clipFC.ShapeType == esriGeometryType.esriGeometryPoint)
                {
                    IFeatureClass inmemPTFC = CreateWithoutDescription("inmemPTFC_" + groupNumber.ToString(), null, inmemFeaWksp, clipFC.ShapeType, flds);
                    InsertFeaturesBoun(inmemPTFC, cursor, extent);

                    IFeatureLayer inmemPTFCLayer = new FeatureLayerClass();
                    inmemPTFCLayer.FeatureClass = inmemPTFC;
                    newGroupLayer.Add(inmemPTFCLayer);
                }

                else if (clipFC.ShapeType == esriGeometryType.esriGeometryPolyline)
                {
                    IFeatureClass inmemLNFC = CreateWithoutDescription("inmemLNFC" + groupNumber.ToString(), null, inmemFeaWksp, clipFC.ShapeType, flds);
                    InsertFeaturesBoun(inmemLNFC, cursor, extent);

                    IFeatureLayer inmemLNFCLayer = new FeatureLayerClass();
                    inmemLNFCLayer.FeatureClass = inmemLNFC;
                    newGroupLayer.Add(inmemLNFCLayer);
                }

                else if (clipFC.ShapeType == esriGeometryType.esriGeometryPolygon)
                {
                    IFeatureClass inmemPLGFC = CreateWithoutDescription("inmemPLGFC" + groupNumber.ToString(), null, inmemFeaWksp, clipFC.ShapeType, flds);
                    InsertFeaturesBoun(inmemPLGFC, cursor, extent);

                    IFeatureLayer inmemPLGFCLayer = new FeatureLayerClass();
                    inmemPLGFCLayer.FeatureClass = inmemPLGFC;
                    newGroupLayer.Add(inmemPLGFCLayer);
                }
            }
            avlbLayers.Add(newGroupLayer);
        }



        public IGeometry GetClipExtent(int extNm)
        {
            IWorkspaceFactory bouFactory = new FileGDBWorkspaceFactoryClass();
            IFeatureWorkspace bouFeaWksp = bouFactory.OpenFromFile(currentDir + "\\" + bouGDB, 0) as IFeatureWorkspace;
            IGeometry clipBoundary = null;

            if (extNm == 0)
            {
                IFeatureClass wrdFC = bouFeaWksp.OpenFeatureClass("原始范围");
                clipBoundary = wrdFC.Search(null, false).NextFeature().ShapeCopy;
            }

            else if (extNm == 1)
            {
                IFeatureClass cnFC = bouFeaWksp.OpenFeatureClass("中国国界");
                clipBoundary = cnFC.Search(null, false).NextFeature().ShapeCopy;
            }

            else if (extNm == 2)
            {
                IFeatureClass prvFC = bouFeaWksp.OpenFeatureClass("省界");
                clipBoundary = prvFC.Search(null, false).NextFeature().ShapeCopy;
            }

            return clipBoundary;
        }

        public IFeatureClass CreateWithoutDescription(String featureClassName, UID classExtensionUID, IFeatureWorkspace featureWorkspace, esriGeometryType geoType, IFields fields)
        {
            // Use IFieldChecker to create a validated fields collection.
            IFieldChecker fieldChecker = new FieldCheckerClass();
            IEnumFieldError enumFieldError = null; 
            IFields validatedFields = null;
            fieldChecker.ValidateWorkspace = (IWorkspace)featureWorkspace;
            fieldChecker.Validate(fields, out enumFieldError, out validatedFields);

            IFeatureClass featureClass = featureWorkspace.CreateFeatureClass
                (featureClassName, validatedFields, null, classExtensionUID,
                esriFeatureType.esriFTSimple, "Shape", ""); 
            return featureClass;
        }

        private static void InsertFeaturesBoun(IFeatureClass newFeatureClass, IFeatureCursor featureCursorSearch, IGeometry clipGeo)
        {
            int intFeatureCount = 0;

            IFeatureCursor featureCursorInsert = newFeatureClass.Insert(true);
            IFeatureBuffer featureBufferInsert = newFeatureClass.CreateFeatureBuffer();

            IFeature feature = featureCursorSearch.NextFeature();
            while (feature != null)
            {
                ITopologicalOperator topoOpe = feature.Shape as ITopologicalOperator;
                IGeometry intersect = topoOpe.Intersect(clipGeo, feature.Shape.Dimension);
                featureBufferInsert.Shape = intersect;

                AddFields(featureBufferInsert, feature);

                featureCursorInsert.InsertFeature(featureBufferInsert);

                if (++intFeatureCount == 100)
                {
                    featureCursorInsert.Flush();
                    intFeatureCount = 0;
                }

                feature = featureCursorSearch.NextFeature();
            }

            featureCursorInsert.Flush();
        }

        private static void AddFields(IFeatureBuffer featureBuffer, IFeature feature)
        {
            // Copy the attributes of the orig feature the new feature
            IRowBuffer rowBuffer = (IRowBuffer)featureBuffer;
            IFields fieldsNew = rowBuffer.Fields;

            IFields fields = feature.Fields;
            for (int i = 0; i <= fields.FieldCount - 1; i++)
            {
                IField field = fields.get_Field(i);
                if ((field.Type != esriFieldType.esriFieldTypeGeometry) &&
                    (field.Type != esriFieldType.esriFieldTypeOID))
                {
                    int intFieldIndex = fieldsNew.FindField(field.Name);
                    if (intFieldIndex != -1)
                    {
                        featureBuffer.set_Value(intFieldIndex, feature.get_Value(i));
                    }
                }
            }
        }

        public int findExtentStatus(int groupNm)
        {
            int theExtStatus = -1;
            if (groupNumber == 1)
                theExtStatus = extentStatus1;
            else if (groupNumber == 2)
                theExtStatus = extentStatus2;
            else if (groupNumber == 3)
                theExtStatus = extentStatus3;
            return theExtStatus;
        }
            
        public bool CheckAvailability(int groupNm, int extNm)
        {
            bool exist = false;
            string checkName = "grp" + groupNm.ToString() + "ext" + extNm.ToString();

            foreach (var listItem in avlbLayers)
            {
                if (listItem.Name == checkName)
                {
                    exist = true;
                    break;
                }

                else
                    exist = false;
            }
            return exist;
        }

        public void UpdateExtentStatus(int groupNm, int extNm)
        {
            if (groupNm == 1)
                extentStatus1 = extNm;
            else if (groupNm == 2)
                extentStatus2 = extNm;
            else if (groupNm == 3)
                extentStatus3 = extNm;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Clear();
            int currentExtent = comboBox1.SelectedIndex;
            UpdateExtentStatus(listBox1.SelectedIndex + 1, currentExtent);
            
            bool exist = CheckAvailability(listBox1.SelectedIndex + 1, currentExtent);

            string findName = "grp" + groupNumber.ToString() + "ext" + currentExtent.ToString();

            if (exist == true)
            {
                Clear();
                IGroupLayer viewGrpLys = avlbLayers.Find(item => item.Name == findName);
                mapControl.AddLayer(viewGrpLys);
            }
            else
            {
                Clear();
                ClipByExtent(groupNumber, currentExtent);
                IGroupLayer newGrpLys = avlbLayers.Find(item => item.Name == findName);
                mapControl.AddLayer(newGrpLys);
            }
        }

        public void Clear()
        {
            mapControl.ActiveView.Clear();
            mapControl.ActiveView.Refresh();
        }

        //动态图例
        private void button1_Click(object sender, EventArgs e)
        {
            var pagelayoutForm = new pageForm();
            pagelayoutForm.Show();
        }

        //创建GDB
        private void button2_Click(object sender, EventArgs e)
        {
            Clear();
            mapControl.Map.DelayDrawing(true);
            IWorkspace pWS;
            DateTime dt1 = DateTime.Now;
            pWS = InMemory.CreateGDB("esriDataSourcesGDB.FileGDBWorkspaceFactory", "test.gdb", System.AppDomain.CurrentDomain.BaseDirectory);
            InMemory.CreateFCs(pWS, mapControl.Map);
            DateTime dt2 = DateTime.Now;
            pWS = InMemory.CreateGDB("esriDataSourcesGDB.InMemoryWorkspaceFactory", "Mygdb", "");
            InMemory.CreateFCs(pWS, mapControl.Map);
            DateTime dt3 = DateTime.Now;
            mapControl.Map.DelayDrawing(false);
            mapControl.ActiveView.Extent = mapControl.ActiveView.FullExtent;
            MessageBox.Show(String.Format("添加3千条记录:\r\nFGDB时间:{0};\r\nInMemory时间:{1};", (dt2 - dt1).TotalSeconds.ToString(), (dt3 - dt2).TotalSeconds.ToString()));            
        }
    }
}