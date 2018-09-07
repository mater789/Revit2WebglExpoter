using Autodesk.Revit.DB;
using System.Collections.Generic;
using Web3DModelWriterManage;

namespace Revit2WebGlExporter.Exporter
{
    class GeometryExporter
    {
        static private List<Va3cContainer.Va3cGeometry> Geometries = new List<Va3cContainer.Va3cGeometry>();

        static public void Initial()
        {
            Geometries = new List<Va3cContainer.Va3cGeometry>();
        }
        
        static public bool ExportGeometryObject(List<GeometryObject> rvtGeometryObjects, ref Va3cContainer.Va3cObject elementObject, Document document)
        {
            Dictionary<ElementId, List<Face>> faceGroups = new Dictionary<ElementId, List<Face>>();
            Dictionary<ElementId, List<Mesh>> meshGroups = new Dictionary<ElementId, List<Mesh>>();  //按材质分组
            List<Curve> curveGroup = new List<Curve>();
            List<PolyLine> polylineGroup = new List<PolyLine>();
            foreach (GeometryObject rvtGeometryObject in rvtGeometryObjects)
                ClassifyGeometryObjects(rvtGeometryObject, document, ref faceGroups, ref meshGroups, ref curveGroup, ref polylineGroup);

            //导出face
            foreach (KeyValuePair<ElementId, List<Face>> faceGroup in faceGroups)
            {
                Va3cContainer.Va3cObject faceGroupObject = new Va3cContainer.Va3cObject();
                faceGroupObject.uuid = StringConverter.NewGuid();
                faceGroupObject.type = "Mesh";
                faceGroupObject.name = "Face Group";
                Material material = document.GetElement(faceGroup.Key) as Material;
                Va3cContainer.Va3cGeometry faceGroupGeometry = new Va3cContainer.Va3cGeometry();
                faceGroupGeometry.uuid = StringConverter.NewGuid();
                faceGroupGeometry.data.nGeometryCategory = eGeometryCategory.Triangle;
                faceGroupObject.geometry = faceGroupGeometry.uuid;
                bool bHasUvs;
                if (ExportFaces(faceGroup.Value, ref faceGroupGeometry, out bHasUvs))
                {
                    if (bHasUvs)
                        MaterialExporter.ExportMaterial(material, typeof(Face), ref faceGroupObject.material);
                    else
                        MaterialExporter.ExportMaterial(material, typeof(Mesh), ref faceGroupObject.material);
                    elementObject.children.Add(faceGroupObject);
                    Geometries.Add(faceGroupGeometry);
                }
            }


            //导出mesh
            foreach (KeyValuePair<ElementId, List<Mesh>> meshGroup in meshGroups)
            {
                Va3cContainer.Va3cObject meshGroupObject = new Va3cContainer.Va3cObject();
                meshGroupObject.uuid = StringConverter.NewGuid();
                meshGroupObject.type = "Mesh";
                meshGroupObject.name = "Mesh Group";
                Material material = document.GetElement(meshGroup.Key) as Material;
                MaterialExporter.ExportMaterial(material, typeof(Mesh), ref meshGroupObject.material);
                Va3cContainer.Va3cGeometry meshGroupGeometry = new Va3cContainer.Va3cGeometry();
                meshGroupGeometry.uuid = StringConverter.NewGuid();
                meshGroupGeometry.data.nGeometryCategory = eGeometryCategory.Triangle;
                meshGroupObject.geometry = meshGroupGeometry.uuid;

                if (ExportMeshes(meshGroup.Value, ref meshGroupGeometry))
                {
                    elementObject.children.Add(meshGroupObject);
                    Geometries.Add(meshGroupGeometry);
                }
            }

            //导出edge
            Va3cContainer.Va3cObject edgeGroupObject = new Va3cContainer.Va3cObject();
            edgeGroupObject.uuid = StringConverter.NewGuid();
            edgeGroupObject.type = "LinePieces";
            edgeGroupObject.name = "Edge Group";
            MaterialExporter.ExportMaterial(null, typeof(Edge), ref edgeGroupObject.material);
            Va3cContainer.Va3cGeometry edgeGroupGeometry = new Va3cContainer.Va3cGeometry();
            edgeGroupGeometry.uuid = StringConverter.NewGuid();
            edgeGroupGeometry.data.nGeometryCategory = eGeometryCategory.Line;
            edgeGroupObject.geometry = edgeGroupGeometry.uuid;
            if (ExportCurveAndPoliLines(curveGroup, polylineGroup, ref edgeGroupGeometry))
            {
                elementObject.children.Add(edgeGroupObject);
                Geometries.Add(edgeGroupGeometry);
            }

            return true;
        }

        static public void GetAllGeometries(ref List<Va3cContainer.Va3cGeometry> outList)
        {
            outList.AddRange(Geometries);
        }

        static private void ClassifyGeometryObjects(GeometryObject rvtGeometryObject, Document document, ref Dictionary<ElementId, List<Face>> faceGroups, ref Dictionary<ElementId, List<Mesh>> meshGroups, ref List<Curve> curveGroup, ref List<PolyLine> polylineGroup)
        {
            if (rvtGeometryObject is Solid)
            {
                Solid solid = rvtGeometryObject as Solid;
                foreach (Face face in solid.Faces)
                {
                    ElementId materialId = face.MaterialElementId;
                    if (faceGroups.ContainsKey(materialId))
                        faceGroups[materialId].Add(face);
                    else
                        faceGroups.Add(materialId, new List<Face> { face });
                }
                foreach (Edge edge in solid.Edges)
                    curveGroup.Add(edge.AsCurve());
            }
            else if (rvtGeometryObject is Face)
            {
                Face face = rvtGeometryObject as Face;
                ElementId materialId = face.MaterialElementId;
                if (faceGroups.ContainsKey(materialId))
                    faceGroups[materialId].Add(face);
                else
                    faceGroups.Add(materialId, new List<Face> { face });
            }
            else if (rvtGeometryObject is Mesh)
            {
                Mesh mesh = rvtGeometryObject as Mesh;
                ElementId materialId = mesh.MaterialElementId;
                if (meshGroups.ContainsKey(materialId))
                    meshGroups[materialId].Add(mesh);
                else
                    meshGroups.Add(materialId, new List<Mesh> { mesh });
            }
            else if (rvtGeometryObject is Curve)
            {
                curveGroup.Add(rvtGeometryObject as Curve);
            }
            else if (rvtGeometryObject is PolyLine)
            {
                PolyLine polyline = rvtGeometryObject as PolyLine;
                polylineGroup.Add(polyline);
            }
            else if (rvtGeometryObject is Profile)
            {
                Profile profile = rvtGeometryObject as Profile;
                foreach (Curve curve in profile.Curves)
                    curveGroup.Add(curve);
            }
        }

        static private bool ExportFaces(List<Face> faces, ref Va3cContainer.Va3cGeometry faceGroupGeometry, out bool bHasUvs)
        {
            bHasUvs = true;

            foreach (Face face in faces)
            {
                Mesh mesh = face.Triangulate(ExportEventHandler.Settings.TriangulateDetailLevel);
                Surface surface = face.GetSurface();
                if (mesh == null || surface == null)
                    continue;

                int currentPointNum = faceGroupGeometry.data.points;
                int currentTriNum = faceGroupGeometry.data.triangles;

                IList<XYZ> points = mesh.Vertices;
                int pointNum = points.Count;
                int triNum = mesh.NumTriangles;
                faceGroupGeometry.data.points += pointNum;
                faceGroupGeometry.data.triangles += triNum;

                foreach (XYZ point in points)
                {
                    faceGroupGeometry.data.vertices.Add((float)point.X);
                    faceGroupGeometry.data.vertices.Add((float)point.Y);
                    faceGroupGeometry.data.vertices.Add((float)point.Z);

                    try
                    {
                        if (bHasUvs)
                        {
                            UV uv;
                            double distance;
                            surface.Project(point, out uv, out distance);
                            faceGroupGeometry.data.uvs.Add((float)uv.U);
                            faceGroupGeometry.data.uvs.Add((float)uv.V);
                            XYZ normal = face.ComputeNormal(uv);
                            faceGroupGeometry.data.normals.Add((float)normal.X);
                            faceGroupGeometry.data.normals.Add((float)normal.Y);
                            faceGroupGeometry.data.normals.Add((float)normal.Z);
                        }
                        else
                        {
                            faceGroupGeometry.data.uvs.Add(0.0f);
                            faceGroupGeometry.data.uvs.Add(0.0f);
                            faceGroupGeometry.data.normals.Add(0.0f);
                            faceGroupGeometry.data.normals.Add(0.0f);
                            faceGroupGeometry.data.normals.Add(1.0f);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        bHasUvs = false;
                        faceGroupGeometry.data.uvs.Add(0.0f);
                        faceGroupGeometry.data.uvs.Add(0.0f);
                        faceGroupGeometry.data.normals.Add(0.0f);
                        faceGroupGeometry.data.normals.Add(0.0f);
                        faceGroupGeometry.data.normals.Add(1.0f);
                    }
                }
                for (int i = 0; i < triNum; ++i)
                {
                    MeshTriangle meshTri = mesh.get_Triangle(i);
                    int index0 = (int)meshTri.get_Index(0) + currentPointNum;
                    int index1 = (int)meshTri.get_Index(1) + currentPointNum;
                    int index2 = (int)meshTri.get_Index(2) + currentPointNum;
                    faceGroupGeometry.data.indices.Add(index0);
                    faceGroupGeometry.data.indices.Add(index1);
                    faceGroupGeometry.data.indices.Add(index2);
                }
            }

            if (faceGroupGeometry.data.points > 0 && faceGroupGeometry.data.triangles > 0)
                return true;

            return false;
        }

        static private bool ExportMeshes(List<Mesh> meshes, ref Va3cContainer.Va3cGeometry meshGroupGeometry)
        {
            foreach (Mesh mesh in meshes)
            {
                int currentPointNum = meshGroupGeometry.data.points;
                int currentTriNum = meshGroupGeometry.data.triangles;

                IList<XYZ> points = mesh.Vertices;
                int pointNum = points.Count;
                int triNum = mesh.NumTriangles;
                meshGroupGeometry.data.points += pointNum;
                meshGroupGeometry.data.triangles += triNum;
                foreach (XYZ point in points)
                {
                    meshGroupGeometry.data.vertices.Add((float)point.X);
                    meshGroupGeometry.data.vertices.Add((float)point.Y);
                    meshGroupGeometry.data.vertices.Add((float)point.Z);

                    //test
                    meshGroupGeometry.data.normals.Add(0.0f);
                    meshGroupGeometry.data.normals.Add(0.0f);
                    meshGroupGeometry.data.normals.Add(1.0f);
                    meshGroupGeometry.data.uvs.Add(0.0f);
                    meshGroupGeometry.data.uvs.Add(0.0f);
                }
                for (int i = 0; i < triNum; ++i)
                {
                    MeshTriangle meshTri = mesh.get_Triangle(i);
                    int index0 = (int)meshTri.get_Index(0) + currentPointNum;
                    int index1 = (int)meshTri.get_Index(1) + currentPointNum;
                    int index2 = (int)meshTri.get_Index(2) + currentPointNum;
                    meshGroupGeometry.data.indices.Add(index0);
                    meshGroupGeometry.data.indices.Add(index1);
                    meshGroupGeometry.data.indices.Add(index2);
                }
            }

            if (meshGroupGeometry.data.points > 0 && meshGroupGeometry.data.triangles > 0)
                return true;

            return false;
        }

        static private bool ExportCurveAndPoliLines(List<Curve> curves, List<PolyLine> polylines, ref Va3cContainer.Va3cGeometry edgeGroupGeometry)
        {
            foreach (Curve curve in curves)
            {
                int currentPointNum = edgeGroupGeometry.data.points;
                int currentTriNum = edgeGroupGeometry.data.triangles;

                IList<XYZ> points = curve.Tessellate();
                edgeGroupGeometry.data.points += points.Count;
                edgeGroupGeometry.data.triangles += points.Count - 1;

                foreach (XYZ point in points)
                {
                    edgeGroupGeometry.data.vertices.Add((float)point.X);
                    edgeGroupGeometry.data.vertices.Add((float)point.Y);
                    edgeGroupGeometry.data.vertices.Add((float)point.Z);
                }
                for (int i = 0; i < points.Count - 1; ++i)
                {
                    edgeGroupGeometry.data.indices.Add(currentPointNum + i);
                    edgeGroupGeometry.data.indices.Add(currentPointNum + i + 1);
                }
            }

            foreach (PolyLine polyline in polylines)
            {
                int currentPointNum = edgeGroupGeometry.data.points;
                int currentTriNum = edgeGroupGeometry.data.triangles;
                
                IList<XYZ> points = polyline.GetCoordinates();
                edgeGroupGeometry.data.points += points.Count;
                edgeGroupGeometry.data.triangles += points.Count - 1;

                foreach (XYZ point in points)
                {
                    edgeGroupGeometry.data.vertices.Add((float)point.X);
                    edgeGroupGeometry.data.vertices.Add((float)point.Y);
                    edgeGroupGeometry.data.vertices.Add((float)point.Z);
                }
                for (int i = 0; i < points.Count - 1; ++i)
                {
                    edgeGroupGeometry.data.indices.Add(currentPointNum + i);
                    edgeGroupGeometry.data.indices.Add(currentPointNum + i + 1);
                }
            }

            if (edgeGroupGeometry.data.points > 0 && edgeGroupGeometry.data.triangles > 0)
                return true;

            return false;
        }
    }
}
