using Autodesk.Revit.DB;
using System.Collections.Generic;
using Web3DModelWriterManage;

namespace Revit2WebGlExporter.Exporter
{
    class ElementExporter
    {
        static Dictionary<int, Va3cContainer.Va3cObject> _existFamilyInstanceObjects;

        static public void Initial()
        {
            _existFamilyInstanceObjects = new Dictionary<int, Va3cContainer.Va3cObject>();
        }

        static public bool ExportElement(Element element, ref Va3cContainer.Va3cObject categoryObject)
        {
            if (!IsValidElement(element))
                return false;

            Va3cContainer.Va3cObject elementObject = new Va3cContainer.Va3cObject();
            elementObject.uuid = StringConverter.NewGuid();
            elementObject.type = element.GetType().Name;
            elementObject.name = element.Name;

            //export transform matrix
            if (element is FamilyInstance)
            {
                Transform transform = (element as Instance).GetTransform();
                if (!transform.IsIdentity)
                {
                    elementObject.bHasMatrix = true;
                    if (!TransformExporter.ExportTransform(transform, ref elementObject.matrix))
                        return false;
                }
            }

            //FamilyInstance不用重复导出几何数据
            if (element is FamilyInstance)
            {
                int hashcode = GetFamilyInstanceHashCode(element as FamilyInstance);
                if (hashcode != -1 && _existFamilyInstanceObjects.ContainsKey(hashcode))
                {
                    if (CopyObject(ref elementObject, _existFamilyInstanceObjects[hashcode]))
                    {
                        if (elementObject.children.Count > 0)
                        {
                            ExportParameters(element, ref elementObject);
                            categoryObject.children.Add(elementObject);
                        }

                        return true;
                    }
                }
            }

            List<GeometryObject> geometryObjects = new List<GeometryObject>();
            if (!GetGeometryObjects(element, ref geometryObjects))
                return false;
            if (!GeometryExporter.ExportGeometryObject(geometryObjects, ref elementObject, element.Document))
                return false;

            if (elementObject.children.Count > 0)
            {
                ExportParameters(element, ref elementObject);
                categoryObject.children.Add(elementObject);
            }

            if (element is FamilyInstance)
            {
                int hashcode = GetFamilyInstanceHashCode(element as FamilyInstance);
                if (hashcode != -1 && !_existFamilyInstanceObjects.ContainsKey(hashcode))
                    _existFamilyInstanceObjects.Add(hashcode, elementObject);
            }

            return true;
        }

        static private void ExportParameters(Element element, ref Va3cContainer.Va3cObject elementObject)
        {
            string propertyName = string.Empty;
            ElementType elementType = element.Document.GetElement(element.GetTypeId()) as ElementType;
            if (elementType != null && string.IsNullOrEmpty(elementType.FamilyName))
                propertyName += (elementType.FamilyName + " " + element.Name);
            else
                propertyName = element.Name;

            string propertyFileName;
            if (PropertyExporter.ExportParameters(propertyName, elementObject.uuid, element.ParametersMap, out propertyFileName))
                elementObject.propertyfile = propertyFileName;
        }

        static private int GetFamilyInstanceHashCode(FamilyInstance instance)
        {
            if (instance == null)
                return -1;

            GeometryElement geometryElement = instance.GetOriginalGeometry(new Options() { DetailLevel = (ViewDetailLevel)ExportEventHandler.Settings.DetailLevel });
            if (geometryElement == null)
                return -1;

            return geometryElement.GetHashCode();
        }

        static private bool GetGeometryObjects(Element element, ref List<GeometryObject> geometryObjects)
        {
            GeometryElement geometryElement = null;

            if (element is FamilyInstance)
                geometryElement = (element as FamilyInstance).GetOriginalGeometry(new Options() { DetailLevel = (ViewDetailLevel)ExportEventHandler.Settings.DetailLevel });
            else
                geometryElement = element.get_Geometry(new Options() { DetailLevel = (ViewDetailLevel)ExportEventHandler.Settings.DetailLevel, ComputeReferences = true });

            geometryObjects = new List<GeometryObject>();
            if (!RecursionObject(geometryElement, ref geometryObjects))
                return false;

            return geometryObjects.Count > 0;
        }

        static private bool RecursionObject(GeometryElement geometryElement, ref List<GeometryObject> geometryObjects)
        {
            if (geometryElement == null)
                return false;

            IEnumerator<GeometryObject> geometryObjectEnum = geometryElement.GetEnumerator();
            while (geometryObjectEnum.MoveNext())
            {
                GeometryObject currentGeometryObject = geometryObjectEnum.Current;
                var type = currentGeometryObject.GetType();
                if (type.Equals(typeof(GeometryInstance)))
                {
                    RecursionObject((currentGeometryObject as GeometryInstance).GetInstanceGeometry(), ref geometryObjects);
                }
                else if (type.Equals(typeof(GeometryElement)))
                {
                    RecursionObject(currentGeometryObject as GeometryElement, ref geometryObjects);
                }
                else/* if (type.Equals(typeof(Solid)))*/
                    geometryObjects.Add(currentGeometryObject);
            }

            return true;
        }

        static private bool CopyObject(ref Va3cContainer.Va3cObject destObject, Va3cContainer.Va3cObject srcObject)
        {
            if (destObject == null || srcObject == null)
                return false;

            destObject.type = srcObject.type;
            destObject.name = srcObject.name;
            destObject.material = srcObject.material;
            destObject.geometry = srcObject.geometry;

            foreach (var child in srcObject.children)
            {
                Va3cContainer.Va3cObject destChild = new Va3cContainer.Va3cObject();
                destChild.uuid = StringConverter.NewGuid();
                if (!CopyObject(ref destChild, child))
                    return false;
                destObject.children.Add(destChild);
            }

            return true;
        }

        static private bool IsValidElement(Element element)
        {
            if (element == null)
                return false;

            if (!ExportEventHandler.Settings.ExportCADLink && element is ImportInstance)
                return false;

            return true;
        }
    }
}
