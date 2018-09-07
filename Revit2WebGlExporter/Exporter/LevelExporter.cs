using Autodesk.Revit.DB;
using System.Collections.Generic;
using Web3DModelWriterManage;

namespace Revit2WebGlExporter.Exporter
{
    class LevelExporter
    {
        static public bool ExportLevels(Dictionary<ElementId, List<Element>> levelElementsDic, ref Va3cContainer.Va3cObject rootObject, Document activeDocument)
        {
            KeyValuePair<ElementId, List<Element>>[] sortedArr;
            SortLevelDictionary(levelElementsDic, out sortedArr, activeDocument);

            foreach (KeyValuePair<ElementId, List<Element>> levelElements in sortedArr)
            {
                if (!ExportLevel(levelElements, ref rootObject, activeDocument))
                    continue;
                else { /*write log*/ }
            }

            return rootObject.children.Count > 0;
        }

        static private bool ExportLevel(KeyValuePair<ElementId, List<Element>> levelElements, ref Va3cContainer.Va3cObject rootObject, Document activeDocument)
        {
            Va3cContainer.Va3cObject levelObject = new Va3cContainer.Va3cObject();
            levelObject.uuid = StringConverter.NewGuid();
            levelObject.type = "Level";
            Level level = activeDocument.GetElement(levelElements.Key) as Level;
            if (level != null)
                levelObject.name = level.Name;
            else
                levelObject.name = "无标高";

            Dictionary<ElementId, List<Element>> CategoryElementsDic = new Dictionary<ElementId, List<Element>>();
            foreach (Element element in levelElements.Value)
            {
                Category category = element.Category;
                ElementId categoryId = new ElementId(-1);
                if (category != null)
                    categoryId = category.Id;
                else
                    categoryId = new ElementId(-1);

                if (CategoryElementsDic.ContainsKey(categoryId))
                    CategoryElementsDic[categoryId].Add(element);
                else
                    CategoryElementsDic.Add(categoryId, new List<Element> { element });
            }

            if (!CategoryExporter.ExportCategories(CategoryElementsDic, ref levelObject, activeDocument))
                return false;

            if (level != null)
            {
                string propertyFileName;
                if (PropertyExporter.ExportParameters(levelObject.name, levelObject.uuid, level.ParametersMap, out propertyFileName))
                    levelObject.propertyfile = propertyFileName;
            }

            if (levelObject.children.Count > 0)
                rootObject.children.Add(levelObject);

            return true;
        }

        static private void SortLevelDictionary(Dictionary<ElementId, List<Element>> levelElementsDic, out KeyValuePair<ElementId, List<Element>>[] sortedArr, Document activeDocument)
        {
            sortedArr = new KeyValuePair<ElementId, List<Element>>[levelElementsDic.Count];
            int index = 0;
            foreach (KeyValuePair<ElementId, List<Element>> levelElements in levelElementsDic)
                sortedArr[index++] = levelElements;

            for (int i = 0; i < sortedArr.Length - 1; i++)
            {
                for (int j = 0; j < sortedArr.Length - 1 - i; j++)
                {
                    if (IsLevel1HigherThanLevel2(sortedArr[j].Key, sortedArr[j+1].Key, activeDocument))
                    {
                        KeyValuePair<ElementId, List<Element>>  temp = sortedArr[j + 1];
                        sortedArr[j + 1] = sortedArr[j];
                        sortedArr[j] = temp;
                    }
                }
            }
        }

        static private bool IsLevel1HigherThanLevel2(ElementId level1Id, ElementId level2Id, Document activeDocument)
        {
            Level level1 = activeDocument.GetElement(level1Id) as Level;
            double level1Elevation = level1 == null ? double.MaxValue : level1.Elevation;

            Level level2 = activeDocument.GetElement(level2Id) as Level;
            double level2Elevation = level2 == null ? double.MaxValue : level2.Elevation;

            return level1Elevation > level2Elevation;
        }
    }
}
