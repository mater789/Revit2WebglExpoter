using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace Revit2WebGlExporter.Exporter
{
    class ViewHelper
    {
        static public bool Set3DView(UIDocument uiDoc)
        {
            if (null == uiDoc.ActiveView || !(uiDoc.ActiveView is View3D))
            {
                FilteredElementCollector collector = new FilteredElementCollector(uiDoc.Document);

                collector.OfClass(typeof(View));

                IEnumerable<View> views
                  = from View view in collector
                    where (view.ViewType == ViewType.ThreeD && !view.IsTemplate)
                    select view;

                foreach (View view in views)
                {
                    if (!view.IsTemplate)
                    {
                        View3D defaultView3D = view as View3D;
                        if (null != defaultView3D)
                        {
                            uiDoc.ActiveView = defaultView3D;
                            return true;
                        }
                    }
                }

                //没有三维视图
                return false;
            }

            return true;
        }

        static public bool SetFirst3DView(UIDocument uiDoc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(uiDoc.Document);
            collector.OfClass(typeof(View));

            IEnumerable<View> views
                = from View view in collector
                where (view.ViewType == ViewType.ThreeD && !view.IsTemplate)
                select view;

            foreach (View view in views)
            {
                if (!view.IsTemplate)
                {
                    View3D defaultView3D = view as View3D;
                    if (null != defaultView3D)
                    {
                        uiDoc.ActiveView = defaultView3D;
                        return true;
                    }
                }
            }

            //没有三维视图
            return false;
        }
    }
}
