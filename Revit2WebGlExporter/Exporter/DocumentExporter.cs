using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.IO;
using Web3DModelWriterManage;

namespace Revit2WebGlExporter.Exporter
{
    class DocumentExporter
    {
        private Va3cContainer Container;

        public DocumentExporter(Application app)
        {
            Container = new Va3cContainer();
            ElementExporter.Initial();
            MaterialExporter.Initial(app);
            GeometryExporter.Initial();
            PropertyExporter.Initial();
        }

        public enum ErrorType
        {
            Success = 0,
            NoElement = -7,
            UnSupportedGeometry = -8,
            Others = -1000
        }

        public ErrorType ExportDocumentWithAcvtiveView(Document document)
        {
            string exportFolder = ExportEventHandler.Settings.OutputFolder;
            if (document == null || !Directory.Exists(exportFolder))
                return ErrorType.Others;

            Container.metadata.generator = "NDS";
            Container.metadata.type = "Object";
            Container.metadata.version = "1.0";
            Container.va3cobject.uuid = StringConverter.NewGuid();
            Container.va3cobject.type = "Root Object";
            Container.va3cobject.name = StringConverter.ToUtf8(Path.GetFileName(document.PathName));
            Container.va3cobject.bHasMatrix = true;
            TransformExporter.GetRootObjectMatrix(ref Container.va3cobject.matrix);

            //Constructs a new FilteredElementCollector that will search and filter the visible elements in a view. 
            FilteredElementCollector elems = new FilteredElementCollector(document, document.ActiveView.Id).WhereElementIsNotElementType();
            if (elems.GetElementCount() == 0)
                return ErrorType.NoElement;

            Dictionary<ElementId, List<Element>> elementsDic = new Dictionary<ElementId, List<Element>>();
            ClassifyElementsByStructureType(elems, ref elementsDic);
            if (ExportEventHandler.Settings.StructureType == CommonSettings.StructureTreeType.ByLevel)
            {
                if (!LevelExporter.ExportLevels(elementsDic, ref Container.va3cobject, document))
                    return ErrorType.NoElement;
            }
            else if (ExportEventHandler.Settings.StructureType == CommonSettings.StructureTreeType.ByCategory)
            {
                if (!CategoryExporter.ExportCategories(elementsDic, ref Container.va3cobject, document))
                    return ErrorType.NoElement;
            }

            MaterialExporter.GetAllMaterials(ref Container.materials);
            MaterialExporter.CopyTextureFiles(exportFolder);
            GeometryExporter.GetAllGeometries(ref Container.geometries);
            PropertyExporter.WritePropertyFiles(exportFolder);
            if (Container.va3cobject.children.Count == 0 || Container.geometries.Count == 0)
                return ErrorType.NoElement;

            if (!Web3DModelWriterManage.Web3DModelWriterManage.WriteWeb3DModelFilesVersion7BufferChunksBvh(ref Container, exportFolder, "model.js", "geom.bin", 2))
                return ErrorType.UnSupportedGeometry;

            return ErrorType.Success; ;
        }

        static private void ClassifyElementsByStructureType(FilteredElementCollector elems, ref Dictionary<ElementId, List<Element>> elementsDic)
        {

            switch (ExportEventHandler.Settings.StructureType)
            {
                case CommonSettings.StructureTreeType.ByLevel:
                    {
                        foreach (Element element in elems)
                        {
                            ElementId levelId = element.LevelId;
                            if (elementsDic.ContainsKey(levelId))
                                elementsDic[levelId].Add(element);
                            else
                                elementsDic.Add(levelId, new List<Element> { element });

                        }
                        break;
                    }
                case CommonSettings.StructureTreeType.ByCategory:
                    {
                        foreach (Element element in elems)
                        {
                            Category category = element.Category;
                            ElementId categoryId = new ElementId(-1);
                            if (category != null)
                                categoryId = category.Id;
                            else
                                categoryId = new ElementId(-1);

                            if (elementsDic.ContainsKey(categoryId))
                                elementsDic[categoryId].Add(element);
                            else
                                elementsDic.Add(categoryId, new List<Element> { element });

                        }
                        break;
                    }
                default:
                    break;
            }
        }
    }
}
