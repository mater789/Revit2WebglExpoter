using System;
using Newtonsoft.Json;

namespace Revit2WebGlExporter
{
    public class CommonSettings
    {
        public string InputFilePath { get; set; }
        public string OutputFolder { get; set; }

        public enum ViewDetailLevel
        {
            Undefined = 0,
            Coarse = 1,
            Medium = 2,
            Fine = 3
        }
        public ViewDetailLevel DetailLevel
        {
            get { return _DetailLevel; }
            set { _DetailLevel = value; }
        }
        private ViewDetailLevel _DetailLevel = ViewDetailLevel.Fine;

        //The level of detail. Its range is from 0 to 1. 0 is the lowest level of detail and 1 is the highest.
        public double TriangulateDetailLevel
        {
            get { return _TriangulateDetailLevel; }
            set
            {
                if (value > 1.0)
                    _TriangulateDetailLevel = 1.0;
                else if (value < 0.0)
                    _TriangulateDetailLevel = 0.0;
                else
                    _TriangulateDetailLevel = value;
            }
        }
        private double _TriangulateDetailLevel = 1.0;

        public enum StructureTreeType
        {
            ByLevel = 0,
            ByCategory
        }
        public StructureTreeType StructureType
        {
            get { return _StructureType; }
            set { _StructureType = value; }
        }
        private StructureTreeType _StructureType = StructureTreeType.ByLevel;

        public bool UseRenderMaterial
        {
            get { return _UseRenderMaterial; }
            set { _UseRenderMaterial = value; }
        }
        private bool _UseRenderMaterial = true;

        /// <summary>
        /// 将对象序列化为二进制数据 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string SerializeToJson()
        {
            string str = JsonConvert.SerializeObject(this);
            return str;
        }

        static public CommonSettings DeserializeWithJson(string str)
        {
            object obj = JsonConvert.DeserializeObject<CommonSettings>(str);
            return (CommonSettings)obj;
        }
    }
}
