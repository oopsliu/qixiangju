using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.esriSystem;

namespace EngineWindowsApplication1
{
    public partial class pageForm : Form
    {
        public static bool auto = false;
        public pageForm()
        {
            InitializeComponent();
            string mxdPath = System.IO.Directory.GetCurrentDirectory() + "\\Data\\Legend\\legend.mxd";
            axPageLayoutControl1.LoadMxFile(mxdPath);
        }

        private void LegendBtn_Click(object sender, EventArgs e)
        {
            if (auto == false)
            {
                auto = true;
                EnableDynamicLegend(auto);
                LegendBtn.Text = "关闭动态图例";
                axPageLayoutControl1.ActiveView.Refresh();
            }
            else
            {
                auto = false;
                EnableDynamicLegend(auto);
                LegendBtn.Text = "开启动态图例";
                axPageLayoutControl1.ActiveView.Refresh();
            }           
        }

        private void EnableDynamicLegend(bool auto)
        {
            var pAC = axPageLayoutControl1.PageLayout as IActiveView;
            for (int i = 0; i < pAC.FocusMap.MapSurroundCount; i++)
            {
                var pSurround = pAC.FocusMap.get_MapSurround(i);
                var pLegend = pSurround as ILegend;
                if (pLegend != null)
                {
                    for (int j = 0; j < pLegend.ItemCount; j++)
                    {
                        var pDyLegendItem = pLegend.get_Item(j) as IDynamicLegendItem;
                        if (pDyLegendItem != null)
                        {
                            pDyLegendItem.AutoVisibility = auto;
                        }
                    }
                }
            }
        }

        private void axPageLayoutControl1_OnMouseDown(object sender, IPageLayoutControlEvents_OnMouseDownEvent e)
        {

        }

    }
}
