using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2WebGlExporter.Exporter
{
    class StringConverter
    {
        static public string NewGuid()
        {
            return Guid.NewGuid().ToString("n");
        }

        static public string ToUtf8(string unicodeStr)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] encodedBytes = utf8.GetBytes(unicodeStr);
            string utf8String = utf8.GetString(encodedBytes);
            return utf8String;
        }
    }
}
