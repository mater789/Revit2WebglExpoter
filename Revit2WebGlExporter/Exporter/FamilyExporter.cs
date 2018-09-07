using Autodesk.Revit.DB;
using System.Collections.Generic;
using Web3DModelWriterManage;

namespace Revit2WebGlExporter.Exporter
{
    class FamilyExporter
    {
        static public bool ExportFamilys(Dictionary<string, List<Element>> familyElementsDic, ref Va3cContainer.Va3cObject categoryObject)
        {
            foreach (var familyElements in familyElementsDic)
            {
                if (!ExportFamily(familyElements, ref categoryObject))
                    continue;
                else { /*write log*/ }
            }

            return true;
        }

        static private bool ExportFamily(KeyValuePair<string, List<Element>> familyElements, ref Va3cContainer.Va3cObject categoryObject)
        {
            if (familyElements.Value.Count == 0)
                return false;

            Va3cContainer.Va3cObject familyObject = new Va3cContainer.Va3cObject();
            familyObject.uuid = StringConverter.NewGuid();
            familyObject.type = "Family";
            familyObject.name = familyElements.Key;

            Dictionary<string, List<Element>> familySymbolElementsDic = new Dictionary<string, List<Element>>();
            List<Element> noFamilySymbolElementsList = new List<Element>();
            foreach (Element element in familyElements.Value)
            {
                string familySymbolName;
                if (GetElementFamilySymbolName(element, out familySymbolName))
                {
                    if (familySymbolElementsDic.ContainsKey(familySymbolName))
                        familySymbolElementsDic[familySymbolName].Add(element);
                    else
                        familySymbolElementsDic.Add(familySymbolName, new List<Element> { element });
                }
                else
                    noFamilySymbolElementsList.Add(element);
            }
            ExportFamilySymbols(familySymbolElementsDic, ref familyObject);

            foreach (Element element in noFamilySymbolElementsList)
            {
                if (!ElementExporter.ExportElement(element, ref familyObject))
                { /*log*/ }
            }

            if (familyObject.children.Count > 0)
                categoryObject.children.Add(familyObject);

            return true;
        }

        static private bool GetElementFamilySymbolName(Element element, out string familySymbolName)
        {
            familySymbolName = string.Empty;

            if (element == null)
                return false;

            ElementType elementType = element.Document.GetElement(element.GetTypeId()) as ElementType;
            if (elementType != null && !string.IsNullOrEmpty(elementType.Name))
            {
                familySymbolName = elementType.Name;
                return true;
            }
            return false;
        }

        static private bool ExportFamilySymbols(Dictionary<string, List<Element>> familySymbolElementsDic, ref Va3cContainer.Va3cObject familyObject)
        {
            foreach (var familySymbolElements in familySymbolElementsDic)
            {
                if (!ExportFamilySymbol(familySymbolElements, ref familyObject))
                    continue;
                else { /*write log*/ }
            }

            return true;
        }

        static private bool ExportFamilySymbol(KeyValuePair<string, List<Element>> familySymbolElements, ref Va3cContainer.Va3cObject familyObject)
        {
            Va3cContainer.Va3cObject familySymbolObject = new Va3cContainer.Va3cObject();
            familySymbolObject.uuid = StringConverter.NewGuid();
            familySymbolObject.type = "Family Symbol";
            familySymbolObject.name = familySymbolElements.Key;

            foreach (Element element in familySymbolElements.Value)
            {
                if (!ElementExporter.ExportElement(element, ref familySymbolObject))
                { /*log*/ }
            }

            if (familySymbolObject.children.Count > 0)
                familyObject.children.Add(familySymbolObject);

            return true;
        }
    }
}
