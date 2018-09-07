using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Revit2WebGlExporter.Exporter
{
    class PropertyExporter
    {
        static private readonly int MaxPropertiesPerSingleFile = 450;   //控制单个属性文件大小
        static private JObject curentPropertyJsObject;
        static private string currentFileId;
        static private Dictionary<string, JObject> PropertyObjectDic; //  key : property file id, value : property json object

        static public void Initial()
        {
            currentFileId = StringConverter.NewGuid();
            curentPropertyJsObject = new JObject();
            PropertyObjectDic = new Dictionary<string, JObject>();
            PropertyObjectDic.Add(currentFileId, curentPropertyJsObject);
        }

        static public bool ExportParameters(string objectName, string objectId, ParameterMap parameterMap, out string propertyFileName)
        {
            propertyFileName = string.Empty;
            if (parameterMap == null || parameterMap.Size == 0)
                return false;

            if (curentPropertyJsObject.Count == MaxPropertiesPerSingleFile)
            {
                currentFileId = StringConverter.NewGuid();
                curentPropertyJsObject = new JObject();
                PropertyObjectDic.Add(currentFileId, curentPropertyJsObject);
            }

            Dictionary<string, List<string>> propertyCategoryDic = new Dictionary<string, List<string>>();
            var itor = parameterMap.ForwardIterator();
            while (itor.MoveNext())
            {
                Parameter param = itor.Current as Parameter;
                Definition def = param.Definition;
                if (def == null)
                    continue;
                string categoryName = LabelUtils.GetLabelFor(def.ParameterGroup);
                string paramName = def.Name;
                if (string.IsNullOrEmpty(paramName))
                    continue;
                string paramValue = "";
                if (!string.IsNullOrEmpty(param.AsString()))
                    paramValue = param.AsString();
                else if (!string.IsNullOrEmpty(param.AsValueString()))
                    paramValue = param.AsValueString();

                if (propertyCategoryDic.ContainsKey(categoryName))
                {
                    propertyCategoryDic[categoryName].Add(paramName);
                    propertyCategoryDic[categoryName].Add(paramValue);
                }
                else
                    propertyCategoryDic.Add(categoryName, new List<string> { paramName, paramValue });
            }
            
            JObject objectPropertyJObj = new JObject();
            objectPropertyJObj.Add("name", objectName);
            JArray propertyCategoriesJArr = new JArray();
            foreach (var propertyCategory in propertyCategoryDic)
            {
                JObject propertyCategoryJObj = new JObject();
                JArray propertyJArr = new JArray(propertyCategory.Value);
                propertyCategoryJObj.Add(propertyCategory.Key, propertyJArr);
                propertyCategoriesJArr.Add(propertyCategoryJObj);
            }
            objectPropertyJObj.Add("PropertyCategories", propertyCategoriesJArr);
            curentPropertyJsObject.Add(objectId, objectPropertyJObj);

            propertyFileName = currentFileId + ".js.gz";
            return true;
        }

        static public bool WritePropertyFiles(string outFolder)
        {
            foreach (var property in PropertyObjectDic)
            {
                string filePath = Path.Combine(outFolder, property.Key + ".js");
                File.WriteAllText(filePath, property.Value.ToString(Formatting.None));
                Compress(filePath);
            }

            return true;
        }

        static private void Compress(string filePath)
        {
            FileInfo fileToCompress = new FileInfo(filePath);
            using (FileStream originalFileStream = fileToCompress.OpenRead())
            {
                if ((File.GetAttributes(fileToCompress.FullName) & FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
                {
                    using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + ".gz"))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);
                        }
                    }
                }
            }

            fileToCompress.Delete();
        }
    }
}
