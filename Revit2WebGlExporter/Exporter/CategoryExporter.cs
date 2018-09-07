using Autodesk.Revit.DB;
using System.Collections.Generic;
using Web3DModelWriterManage;

namespace Revit2WebGlExporter.Exporter
{
    class CategoryExporter
    {
        static public bool ExportCategories(Dictionary<ElementId, List<Element>> categoryElementsDic, ref Va3cContainer.Va3cObject rootObject, Document activeDocument)
        {
            foreach (var categoryElements in categoryElementsDic)
            {
                if (!ExportCategory(categoryElements, ref rootObject, activeDocument))
                    continue;
                else { /*write log*/ }
            }

            return true;
        }

        static private bool ExportCategory(KeyValuePair<ElementId, List<Element>> categoryElements, ref Va3cContainer.Va3cObject rootObject, Document activeDocument)
        {
            if (categoryElements.Value.Count == 0)
                return false;

            Va3cContainer.Va3cObject categoryObject = new Va3cContainer.Va3cObject();
            categoryObject.uuid = StringConverter.NewGuid();
            categoryObject.type = "Category";
            
            Category category = Category.GetCategory(activeDocument, categoryElements.Key);
            if (category == null)
                categoryObject.name = "无类别";
            else
                categoryObject.name = category.Name;
            
            Dictionary<string, List<Element>> familyElementsDic = new Dictionary<string, List<Element>>();
            List<Element> noFamilyElementsList = new List<Element>();
            foreach (Element element in categoryElements.Value)
            {
                string familyName;
                if (GetElementFamilyName(element, out familyName))
                {
                    if (familyElementsDic.ContainsKey(familyName))
                        familyElementsDic[familyName].Add(element);
                    else
                        familyElementsDic.Add(familyName, new List<Element> { element });
                }
                else
                    noFamilyElementsList.Add(element);
            }
            FamilyExporter.ExportFamilys(familyElementsDic, ref categoryObject);

            foreach (Element element in noFamilyElementsList)
            {
                if (!ElementExporter.ExportElement(element, ref categoryObject))
                { /*log*/ }
            }

            if (categoryObject.children.Count > 0)
                rootObject.children.Add(categoryObject);

            return true;
        }

        static private bool GetElementFamilyName(Element element, out string familyName)
        {
            familyName = string.Empty;

            if (element == null)
                return false;

            ElementType elementType = element.Document.GetElement(element.GetTypeId()) as ElementType;
            if (elementType != null && !string.IsNullOrEmpty(elementType.FamilyName))
            {
                familyName = elementType.FamilyName;
                return true;
            }
            return false;
        }
    }
}
