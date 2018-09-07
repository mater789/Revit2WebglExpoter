using Autodesk.Revit.DB;

namespace Revit2WebGlExporter.Exporter
{
    class TransformExporter
    {
        static public bool ExportTransform(Transform transform, ref float[] matrix)
        {
            XYZ basisX = transform.BasisX;
            XYZ basisY = transform.BasisY;
            XYZ basisZ = transform.BasisZ;
            XYZ origin = transform.Origin;
            double scale = transform.IsConformal ? transform.Scale : 1.0;

            matrix[0] = (float)basisX.X;
            matrix[1] = (float)basisX.Y;
            matrix[2] = (float)basisX.Z;
            matrix[3] = 0;

            matrix[4] = (float)basisY.X;
            matrix[5] = (float)basisY.Y;
            matrix[6] = (float)basisY.Z;
            matrix[7] = 0;

            matrix[8] = (float)basisZ.X;
            matrix[9] = (float)basisZ.Y;
            matrix[10] = (float)basisZ.Z;
            matrix[11] = 0;


            matrix[12] = (float)origin.X;
            matrix[13] = (float)origin.Y;
            matrix[14] = (float)origin.Z;
            matrix[15] = (float)scale;

            return true;
        }

        static public void GetRootObjectMatrix(ref float[] matrix)
        {
            matrix[0] = 1;
            matrix[1] = 0;
            matrix[2] = 0;
            matrix[3] = 0;

            matrix[4] = 0;
            matrix[5] = 0;
            matrix[6] = -1;
            matrix[7] = 0;

            matrix[8] = 0;
            matrix[9] = 1;
            matrix[10] = 0;
            matrix[11] = 0;
            
            matrix[12] = 0;
            matrix[13] = 0;
            matrix[14] = 0;
            matrix[15] = 1;
        }
    }
}
