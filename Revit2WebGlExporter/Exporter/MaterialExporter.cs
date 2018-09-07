using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using Web3DModelWriterManage;

namespace Revit2WebGlExporter.Exporter
{
    public enum eMaterial
    {
        Material_generic = 0,   //通用
        Material_hardwood,      //实木
        Material_masonrycmu,    //砌体
        Material_metal,         //金属
        Material_plasticvinyl,  //塑料
        Material_glazing,       //玻璃
        Material_wallpaint,     //油漆
        Material_solidglass,    //实心玻璃
        Material_concrete,      //混凝土
        Material_mirror,        //镜子
        Material_ceramic,       //陶瓷、瓷砖、楼板
        Material_stone,         //石料、大理石、花岗岩之类的
        Material_water,         //水
        Material_other
    }

    class MaterialExporter
    {
        static private Va3cContainer.Va3cMaterial DefaultFaceMaterial;
        static private Va3cContainer.Va3cMaterial DefaultLineMaterial;
        static private Dictionary<ElementId, Va3cContainer.Va3cMaterial> GraphicsFaceMaterials;
        static private Dictionary<ElementId, Va3cContainer.Va3cMaterial> RenderingFaceMaterials;
        
        static private List<string> AdskTextureLibraryPathList;
        static private List<string> TextureImageFiles;
        static private AssetSet AdskAssetLibrary;

        private const double BUMP_RADIO_HUNDRED = 0.01;
        private const double BUMP_RADIO_THUNSAND = 0.001;

        static public bool Initial(Application app)
        {
            DefaultFaceMaterial = new Va3cContainer.Va3cMaterial();
            DefaultFaceMaterial.uuid = StringConverter.NewGuid();
            DefaultFaceMaterial.ambient = "12632256";
            DefaultFaceMaterial.specular = "12632256";
            DefaultFaceMaterial.shininess = "200";
            DefaultFaceMaterial.reflectivity = "0.6";
            DefaultFaceMaterial.doubleSided = true;
            DefaultFaceMaterial.type = "MeshStandardMaterial";
            DefaultFaceMaterial.roughness = "0.5";
            DefaultFaceMaterial.metalness = "0.5";
            DefaultFaceMaterial.color = "12632256";
            DefaultFaceMaterial.opacity = "1.0";

            DefaultLineMaterial = new Va3cContainer.Va3cMaterial();
            DefaultLineMaterial.uuid = StringConverter.NewGuid();
            DefaultLineMaterial.linewidth = "1";
            DefaultLineMaterial.type = "LineBasicMaterial";
            DefaultLineMaterial.color = "0";
            DefaultLineMaterial.opacity = "1.0";

            GraphicsFaceMaterials = new Dictionary<ElementId, Va3cContainer.Va3cMaterial>();
            RenderingFaceMaterials = new Dictionary<ElementId, Va3cContainer.Va3cMaterial>();

            if (ExportEventHandler.Settings.UseRenderMaterial == true)
            {
                AdskTextureLibraryPathList = new List<string>();
                if (GetAdskTextureLibraryPaths(ref AdskTextureLibraryPathList) == 0)
                    return false;
                TextureImageFiles = new List<string>();
                AdskAssetLibrary = app.get_Assets(AssetType.Appearance);
            }

            return true;
        }

        static private int GetAdskTextureLibraryPaths(ref List<string> AdskTextureLibraryPathList)
        {
            try
            {
                RegistryKey adskAdvancedTextureLibraryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Wow6432Node\Autodesk\ADSKAdvancedTextureLibrary");
                if (adskAdvancedTextureLibraryKey != null)
                {
                    GetLibraryPaths(adskAdvancedTextureLibraryKey, ref AdskTextureLibraryPathList);
                    adskAdvancedTextureLibraryKey.Close();
                }
            }
            catch
            { }

            try
            {
                RegistryKey adskPrismTextureLibraryNew = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Wow6432Node\Autodesk\ADSKPrismTextureLibraryNew");
                if (adskPrismTextureLibraryNew != null)
                {
                    GetLibraryPaths(adskPrismTextureLibraryNew, ref AdskTextureLibraryPathList);
                    adskPrismTextureLibraryNew.Close();
                }
            }
            catch
            { }

            try
            {
                RegistryKey adskTextureLibrary = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Wow6432Node\Autodesk\ADSKTextureLibrary");
                if (adskTextureLibrary != null)
                {
                    GetLibraryPaths(adskTextureLibrary, ref AdskTextureLibraryPathList);
                    adskTextureLibrary.Close();
                }
            }
            catch
            { }

            try
            {
                RegistryKey adskTextureLibraryNew = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Wow6432Node\Autodesk\ADSKTextureLibraryNew");
                if (adskTextureLibraryNew != null)
                {
                    GetLibraryPaths(adskTextureLibraryNew, ref AdskTextureLibraryPathList);
                    adskTextureLibraryNew.Close();
                }
            }
            catch
            { }

            return AdskTextureLibraryPathList.Count;
        }

        static private void GetLibraryPaths(RegistryKey key, ref List<string> pathList)
        {
            if (key == null)
                return;

            string path = (string)key.GetValue("LibraryPaths");
            if (!string.IsNullOrEmpty(path))
            {
                path = path.ToLower();
                if (!pathList.Contains(path))
                    pathList.Add(path);
            }

            string[] subKeyNames = key.GetSubKeyNames();
            if (subKeyNames != null)
            {
                foreach (string subKeyName in subKeyNames)
                {
                    RegistryKey subKey = key.OpenSubKey(subKeyName);
                    GetLibraryPaths(subKey, ref pathList);
                    subKey.Close();
                }
            }
        }

        static public void GetAllMaterials(ref List<Va3cContainer.Va3cMaterial> outList)
        {
            outList.AddRange(GraphicsFaceMaterials.Values);
            outList.AddRange(RenderingFaceMaterials.Values);
            outList.Add(DefaultLineMaterial);
            outList.Add(DefaultFaceMaterial);
        }

        static public bool CopyTextureFiles(string outFolder)
        {
            if (!Directory.Exists(outFolder))
                return false;

            try
            {
                foreach (string filePath in TextureImageFiles)
                {
                    string destFile = Path.Combine(outFolder, Path.GetFileName(filePath));
                    File.Copy(filePath, destFile);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        static public bool ExportMaterial(Material material, System.Type objectType, ref string materialId)
        {
            //线类型使用默认材质
            if (objectType == typeof(Edge))
            {
                materialId = DefaultLineMaterial.uuid;
                return true;
            }

            if (material == null)
            {
                materialId = DefaultFaceMaterial.uuid;
                return true;
            }

            if (objectType == typeof(Face) && ExportEventHandler.Settings.UseRenderMaterial)
            {
                if (ExportRenderingMaterial(material, ref materialId))
                    return true;
                else return ExportGraphicsMaterial(material, ref materialId);
            }
            else
                return ExportGraphicsMaterial(material, ref materialId);
        }

        static private bool IsGraphicsMaterialExsit(string strColor, string strAlpha, string strShininess, string strRoughness, ref string materialId)
        {
            foreach (var material in GraphicsFaceMaterials)
            {
                float color = float.Parse(material.Value.color);
                float opacity = float.Parse(material.Value.opacity);
                float shininess = float.Parse(material.Value.shininess);
                float roughness = float.Parse(material.Value.roughness);

                float icolor = float.Parse(strColor);
                float iopacity = float.Parse(strAlpha);
                float ishininess = float.Parse(strShininess);
                float iroughness = float.Parse(strRoughness);

                if (System.Math.Abs(color - icolor) < 1e-6
                    && System.Math.Abs(opacity - iopacity) < 1e-6
                    && System.Math.Abs(shininess - ishininess) < 1e-6
                    && System.Math.Abs(roughness - iroughness) < 1e-6)
                {
                    materialId = material.Value.uuid;
                    return true;
                }
            }

            return false;
        }

        static private bool ExportGraphicsMaterial(Material material, ref string uuid)
        {
            if (material == null)
            {
                uuid = DefaultFaceMaterial.uuid;
                return true;
            }

            if (GraphicsFaceMaterials.ContainsKey(material.Id))
            {
                uuid = GraphicsFaceMaterials[material.Id].uuid;
                return true;
            }

            byte r = material.Color.Red;
            byte g = material.Color.Green;
            byte b = material.Color.Blue;
            int color = r << 16 | g << 8 | b;
            float alpha = 1.0f - (float)material.Transparency / 100.0f;
            string strColor = color.ToString();
            string strAlpha = alpha.ToString();
            string strShininess = material.Shininess.ToString();
            string strRoughness = (1.0f - (float)material.Smoothness / 100.0f).ToString();

            if (IsGraphicsMaterialExsit(strColor, strAlpha, strShininess, strRoughness, ref uuid))
                return true;
            else
            {
                Va3cContainer.Va3cMaterial va3cMaterial = new Va3cContainer.Va3cMaterial();
                va3cMaterial.uuid = StringConverter.NewGuid();
                va3cMaterial.color = strColor;
                va3cMaterial.opacity = strAlpha;
                va3cMaterial.ambient = strColor;
                va3cMaterial.specular = strColor;
                va3cMaterial.shininess = strShininess;
                va3cMaterial.reflectivity = "0.6";
                va3cMaterial.doubleSided = true;
                va3cMaterial.type = "MeshStandardMaterial";
                va3cMaterial.roughness = strRoughness;
                va3cMaterial.metalness = "0.5";
                if (!GraphicsFaceMaterials.ContainsKey(material.Id))
                    GraphicsFaceMaterials.Add(material.Id, va3cMaterial);

                uuid = va3cMaterial.uuid;

                return true;
            }
        }

        #region 纹理材质

        static private bool ExportRenderingMaterial(Material material, ref string materialId)
        {
            if (RenderingFaceMaterials.ContainsKey(material.Id))
            {
                materialId = RenderingFaceMaterials[material.Id].uuid;
                return true;
            }

            AppearanceAssetElement appearanceAssetElement = material.Document.GetElement(material.AppearanceAssetId) as AppearanceAssetElement;
            if (appearanceAssetElement == null)
                return false;
            Asset renderingAsset = appearanceAssetElement.GetRenderingAsset();
            if (renderingAsset == null)
                return false;

            if (renderingAsset.Size == 0)
            {
                //检索不到材质外观时，为Autodesk材质库材质
                foreach (Asset autodeskAsset in AdskAssetLibrary)
                {
                    if (autodeskAsset.Name.CompareTo(renderingAsset.Name) == 0 && autodeskAsset.LibraryName.CompareTo(renderingAsset.LibraryName) == 0)
                    {
                        renderingAsset = autodeskAsset;
                        break;
                    }
                }
            }

            Va3cContainer.Va3cMaterial va3cMaterial = new Va3cContainer.Va3cMaterial();
            va3cMaterial.uuid = StringConverter.NewGuid();
            if (!GetRenderingAssetInfo(ref va3cMaterial, renderingAsset))
                return false;
            if (!RenderingFaceMaterials.ContainsKey(material.Id))
                RenderingFaceMaterials.Add(material.Id, va3cMaterial);
            materialId = va3cMaterial.uuid;

            return true;
        }

        static private bool GetRenderingAssetInfo(ref Va3cContainer.Va3cMaterial va3cMaterial, Asset asset)
        {
            eMaterial type = GetRenderingMaterialType(asset);
            if (type == eMaterial.Material_other)
                return false;
            switch (type)
            {
                case eMaterial.Material_ceramic:
                    GetCeramicMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_concrete:
                    GetConcreteMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_generic:
                    GetGenericMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_glazing:
                    GetGlazingMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_hardwood:
                    GetHardwoodMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_masonrycmu:
                    GetMasonrycmuMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_metal:
                    GetMetalMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_mirror:
                    GetMirrorMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_plasticvinyl:
                    GetPlasticvinylMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_solidglass:
                    GetSolidglassMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_stone:
                    GetStoneMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_wallpaint:
                    GetWallPaintMaterialInfo(asset, ref va3cMaterial);
                    break;
                case eMaterial.Material_water:
                    GetWaterMaterialInfo(asset, ref va3cMaterial);
                    break;
            }

            if (string.IsNullOrEmpty(va3cMaterial.mapDiffuse) && string.IsNullOrEmpty(va3cMaterial.mapBump) && string.IsNullOrEmpty(va3cMaterial.color))
                return false;

            return true;
        }

        static private eMaterial GetRenderingMaterialType(Asset asset)
        {
            for (int i = 0; i < asset.Size; i++)
            {
                AssetProperty property = asset.Get(i);
                int nIndex = property.Name.IndexOf("generic", StringComparison.OrdinalIgnoreCase);
                eMaterial matDefinition = (nIndex == 0) ? eMaterial.Material_generic : eMaterial.Material_other;

                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("hardwood", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_hardwood : eMaterial.Material_other;
                }

                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("masonrycmu", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_masonrycmu : eMaterial.Material_other;
                }

                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("metal", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_metal : eMaterial.Material_other;
                }


                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("plasticvinyl", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_plasticvinyl : eMaterial.Material_other;
                }

                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("glazing", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_glazing : eMaterial.Material_other;
                }

                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("wallpaint", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_wallpaint : eMaterial.Material_other;
                }

                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("solidglass", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_solidglass : eMaterial.Material_other;
                }

                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("concrete", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_concrete : eMaterial.Material_other;
                }

                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("mirror", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_mirror : eMaterial.Material_other;
                }

                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("ceramic", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_ceramic : eMaterial.Material_other;
                }

                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("stone", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_stone : eMaterial.Material_other;
                }

                if (eMaterial.Material_other == matDefinition)
                {
                    nIndex = property.Name.IndexOf("water", StringComparison.OrdinalIgnoreCase);
                    matDefinition = (nIndex == 0) ? eMaterial.Material_water : eMaterial.Material_other;
                }

                if (matDefinition != eMaterial.Material_other)
                    return matDefinition;
            }

            return eMaterial.Material_other;
        }

        static private bool GetAssetBitmapPath(Asset asset, ref string textureFilePath)
        {
            for (int i = 0; i < asset.Size; i++)
            {
                AssetProperty property = asset.Get(i);
                if (RecursionAssetProperty(property, ref textureFilePath))
                    return true;
            }

            return false;
        }

        static private bool RecursionAssetProperty(AssetProperty property, ref string textureFilePath)
        {
            if (property == null)
                return false;

            if (property.Type == AssetPropertyType.String && (property.Name == "unifiedbitmap_Bitmap"))
            {
                AssetPropertyString assetPropertyString = property as AssetPropertyString;
                if (assetPropertyString != null)
                {
                    string strMaterialImages = assetPropertyString.Value;
                    string[] images = strMaterialImages.Split('|');
                    if (images.Length > 0)
                    {
                        textureFilePath = images[0];

                        if (!File.Exists(textureFilePath))
                            GetTextureRealPath(ref textureFilePath);

                        if (File.Exists(textureFilePath))
                        {
                            if (!TextureImageFiles.Contains(textureFilePath))
                                TextureImageFiles.Add(textureFilePath);

                            return true;
                        }
                    }
                }
            }
            else if (property.Type == AssetPropertyType.Asset)
            {
                if (GetAssetBitmapPath(property as Asset, ref textureFilePath))
                    return true;
            }
            else
            {
                IList<AssetProperty> allConnectedProperties = property.GetAllConnectedProperties();
                foreach (AssetProperty connectedProperty in allConnectedProperties)
                {
                    if (RecursionAssetProperty(connectedProperty, ref textureFilePath))
                        return true;
                }
            }

            return false;
        }

        static private bool GetTextureRealPath(ref string path)
        {
            foreach (string textureFolder in AdskTextureLibraryPathList)
            {
                string fullPath = Path.Combine(textureFolder, path);
                if (File.Exists(fullPath))
                {
                    path = fullPath;
                    return true;
                }
            }

            return false;
        }

        static private void GetCeramicMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            // 纹理贴图
            AssetPropertyDoubleArray4d ceramic_color = asset.FindByName("ceramic_color") as AssetPropertyDoubleArray4d;
            if (null != ceramic_color)
            {
                string textureFilePath = "";
                foreach (AssetProperty subProp in ceramic_color.GetAllConnectedProperties())
                {
                    if (subProp.Type == AssetPropertyType.Asset)
                    {
                        if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                        {
                            va3cMaterial.mapDiffuse = Path.GetFileName(textureFilePath);
                            break;
                        }
                    }
                }

                //不需要颜色
            }

            //浮雕贴图
            AssetProperty ceramic_pattern_map = asset.FindByName("ceramic_pattern_map");
            if (null != ceramic_pattern_map)
            {
                string textureFilePath = "";
                foreach (AssetProperty subProp in ceramic_pattern_map.GetAllConnectedProperties())
                {
                    if (subProp.Type == AssetPropertyType.Asset)
                    {
                        if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                        {
                            va3cMaterial.mapBump = Path.GetFileName(textureFilePath);
                            break;
                        }
                    }
                }

                AssetProperty ceramic_pattern_amount = asset.FindByName("ceramic_pattern_amount");
                if (null != ceramic_pattern_amount)
                {
                    if (ceramic_pattern_amount.Type == AssetPropertyType.Float)
                        va3cMaterial.bumpScale = ((ceramic_pattern_amount as AssetPropertyFloat).Value * BUMP_RADIO_HUNDRED).ToString();
                    else if (ceramic_pattern_amount.Type == AssetPropertyType.Double1)
                        va3cMaterial.bumpScale = ((ceramic_pattern_amount as AssetPropertyDouble).Value * BUMP_RADIO_HUNDRED).ToString();
                }
            }
        }

        static private void GetConcreteMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            //纹理贴图
            AssetProperty concrete_finish = asset.FindByName("concrete_finish");
            if (null != concrete_finish)
            {
                int opt = -1;
                if (concrete_finish.Type == AssetPropertyType.Integer)
                    opt = (concrete_finish as AssetPropertyInteger).Value;
                else if (concrete_finish.Type == AssetPropertyType.Enumeration)
                    opt = (concrete_finish as AssetPropertyEnum).Value;

                if (opt == 4)   // 使用自定义图片
                {
                    AssetProperty concrete_bump_map = asset.FindByName("concrete_bump_map");
                    if (null != concrete_bump_map)
                    {
                        string textureFilePath = "";
                        foreach (AssetProperty subProp in concrete_bump_map.GetAllConnectedProperties())
                        {
                            if (subProp.Type == AssetPropertyType.Asset)
                            {
                                if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                                {
                                    va3cMaterial.mapDiffuse = Path.GetFileName(textureFilePath);
                                    break;
                                }
                            }
                        }
                    }

                    AssetProperty concrete_bump_amount = asset.FindByName("concrete_bump_amount");
                    if (null != concrete_bump_amount)
                    {
                        if (concrete_bump_amount.Type == AssetPropertyType.Float)
                            va3cMaterial.bumpScale = ((concrete_bump_amount as AssetPropertyFloat).Value * BUMP_RADIO_HUNDRED).ToString();
                        else if (concrete_bump_amount.Type == AssetPropertyType.Double1)
                            va3cMaterial.bumpScale = ((concrete_bump_amount as AssetPropertyDouble).Value * BUMP_RADIO_HUNDRED).ToString();
                    }
                }
            }

            //风化
            AssetProperty concrete_brightmode = asset.FindByName("concrete_brightmode");
            if (concrete_brightmode != null)
            {
                int opt = -1;
                if (concrete_brightmode.Type == AssetPropertyType.Integer)
                    opt = (concrete_brightmode as AssetPropertyInteger).Value;
                else if (concrete_brightmode.Type == AssetPropertyType.Enumeration)
                    opt = (concrete_brightmode as AssetPropertyEnum).Value;

                if (opt == 2)   // 使用自定义图片
                {
                    AssetProperty concrete_bm_map = asset.FindByName("concrete_bm_map");
                    if (concrete_bm_map != null)
                    {
                        string textureFilePath = "";
                        foreach (AssetProperty subProp in concrete_bm_map.GetAllConnectedProperties())
                        {
                            if (subProp.Type == AssetPropertyType.Asset)
                            {
                                if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                                {
                                    va3cMaterial.mapBump = Path.GetFileName(textureFilePath);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            //颜色
            AssetPropertyDoubleArray4d concrete_color = asset.FindByName("concrete_color") as AssetPropertyDoubleArray4d;
            if (null != concrete_color)
            {
                IList<double> color = concrete_color.GetValueAsDoubles();
                va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
            }

            AssetPropertyBoolean common_Tint_toggle = asset.FindByName("common_Tint_toggle") as AssetPropertyBoolean;
            if (null != common_Tint_toggle && common_Tint_toggle.Value)
            {
                AssetPropertyDoubleArray4d common_Tint_color = asset.FindByName("common_Tint_color") as AssetPropertyDoubleArray4d;
                if (null != common_Tint_color)
                {
                    IList<double> color = common_Tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                }
            }
        }

        static private void GetGenericMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            //纹理及颜色
            AssetPropertyDoubleArray4d generic_diffuse = asset.FindByName("generic_diffuse") as AssetPropertyDoubleArray4d;
            if (null != generic_diffuse)
            {
                string textureFilePath = "";
                foreach (AssetProperty subProp in generic_diffuse.GetAllConnectedProperties())
                {
                    if (subProp.Type == AssetPropertyType.Asset)
                    {
                        if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                        {
                            va3cMaterial.mapDiffuse = Path.GetFileName(textureFilePath);
                            break;
                        }
                    }
                }

                IList<double> color = generic_diffuse.GetValueAsDoubles();
                va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
            }

            AssetPropertyBoolean common_Tint_toggle = asset.FindByName("common_Tint_toggle") as AssetPropertyBoolean;
            if (null != common_Tint_toggle && common_Tint_toggle.Value)
            {
                AssetPropertyDoubleArray4d common_Tint_color = asset.FindByName("common_Tint_color") as AssetPropertyDoubleArray4d;
                if (null != common_Tint_color)
                {
                    IList<double> color = common_Tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                }
            }

            //凹凸贴图
            AssetPropertyDoubleArray4d generic_bump_map = asset.FindByName("generic_bump_map") as AssetPropertyDoubleArray4d;
            if (null != generic_bump_map)
            {
                string textureFilePath = "";
                foreach (AssetProperty subProp in generic_bump_map.GetAllConnectedProperties())
                {
                    if (subProp.Type == AssetPropertyType.Asset)
                    {
                        if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                        {
                            va3cMaterial.mapBump = Path.GetFileName(textureFilePath);
                            break;
                        }
                    }
                }
            }

            AssetPropertyDouble generic_bump_amount_d = asset.FindByName("generic_bump_amount") as AssetPropertyDouble;
            if (null != generic_bump_amount_d)
                va3cMaterial.bumpScale = (generic_bump_amount_d.Value * BUMP_RADIO_THUNSAND).ToString();
            else
            {
                AssetPropertyFloat generic_bump_amount_f = asset.FindByName("generic_bump_amount") as AssetPropertyFloat;
                if (null != generic_bump_amount_f)
                    va3cMaterial.bumpScale = (generic_bump_amount_f.Value * BUMP_RADIO_THUNSAND).ToString();
            }

            //透明度
            AssetPropertyDouble generic_transparency_d = asset.FindByName("generic_transparency") as AssetPropertyDouble;
            if (null != generic_transparency_d)
                va3cMaterial.opacity = (1.0 - generic_transparency_d.Value).ToString();
            else
            {
                AssetPropertyFloat generic_transparency_f = asset.FindByName("generic_transparency") as AssetPropertyFloat;
                if (null != generic_transparency_f)
                    va3cMaterial.opacity = (1.0 - generic_transparency_f.Value).ToString();
            }

            //其他
            AssetPropertyBoolean generic_is_metal = asset.FindByName("generic_is_metal") as AssetPropertyBoolean;
            if (null != generic_is_metal)
            {
                va3cMaterial.isMetal = generic_is_metal.Value;
                if (va3cMaterial.isMetal)
                    va3cMaterial.shininess = "10";
            }

            AssetPropertyDouble generic_glossiness_d = asset.FindByName("generic_glossiness") as AssetPropertyDouble;
            if (null != generic_glossiness_d)
                va3cMaterial.roughness = (1.0 - generic_glossiness_d.Value).ToString();
            else
            {
                AssetPropertyFloat generic_glossiness_f = asset.FindByName("generic_glossiness") as AssetPropertyFloat;
                if (null != generic_glossiness_f)
                    va3cMaterial.roughness = (1.0 - generic_glossiness_f.Value).ToString();
            }
        }
        
        static private void GetGlazingMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            //颜色, glazing_transmittance_color有时是枚举有时是int
            AssetPropertyEnum glazing_transmittance_color_e = asset.FindByName("glazing_transmittance_color") as AssetPropertyEnum;
            AssetPropertyInteger glazing_transmittance_color_i = asset.FindByName("glazing_transmittance_color") as AssetPropertyInteger;
            int opt = -1;
            if (glazing_transmittance_color_e != null)
                opt = glazing_transmittance_color_e.Value;
            else if (glazing_transmittance_color_i != null)
                opt = glazing_transmittance_color_i.Value;
            switch (opt)
            {
                case 0:
                    va3cMaterial.color = ColorToString(0.858, 0.893, 0.879).ToString();
                    break;
                case 1:
                    va3cMaterial.color = ColorToString(0.676, 0.797, 0.737).ToString();
                    break;
                case 2:
                    va3cMaterial.color = ColorToString(0.451, 0.449, 0.472).ToString();
                    break;
                case 3:
                    va3cMaterial.color = ColorToString(0.367, 0.514, 0.651).ToString();
                    break;
                case 4:
                    va3cMaterial.color = ColorToString(0.654, 0.788, 0.772).ToString();
                    break;
                case 5:
                    va3cMaterial.color = ColorToString(0.583, 0.516, 0.467).ToString();
                    break;
                case 6:
                    AssetPropertyDoubleArray4d color = asset.FindByName("glazing_transmittance_map") as AssetPropertyDoubleArray4d;
                    if (null != color)
                    {
                        IList<double> c = color.GetValueAsDoubles();
                        va3cMaterial.color = ColorToString(c[0], c[1], c[2]);
                    }
                    break;
                default:
                    break;
            }
            
            AssetPropertyBoolean common_Tint_toggle = asset.FindByName("common_Tint_toggle") as AssetPropertyBoolean;
            if (null != common_Tint_toggle && common_Tint_toggle.Value)
            {
                AssetPropertyDoubleArray4d common_Tint_color = asset.FindByName("common_Tint_color") as AssetPropertyDoubleArray4d;
                if (null != common_Tint_color)
                {
                    IList<double> c = common_Tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(c[0], c[1], c[2]);
                }
            }

            AssetPropertyFloat glazing_reflectance_f = asset.FindByName("glazing_reflectance") as AssetPropertyFloat;
            if (null != glazing_reflectance_f)
                va3cMaterial.opacity = glazing_reflectance_f.Value.ToString();
            else
            {
                AssetPropertyDouble glazing_reflectance_d = asset.FindByName("glazing_reflectance") as AssetPropertyDouble;
                if (null != glazing_reflectance_d)
                    va3cMaterial.opacity = glazing_reflectance_d.Value.ToString();
            }
        }

        static private void GetHardwoodMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            //纹理贴图
            AssetProperty hardwood_color = asset.FindByName("hardwood_color");
            if (null != hardwood_color)
            {
                string textureFilePath = "";
                foreach (AssetProperty subProp in hardwood_color.GetAllConnectedProperties())
                {
                    if (subProp.Type == AssetPropertyType.Asset)
                    {
                        if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                        {
                            va3cMaterial.mapDiffuse = Path.GetFileName(textureFilePath);
                            break;
                        }
                    }
                }
            }

            //颜色
            AssetProperty hardwood_tint_enable = asset.FindByName("hardwood_tint_enabled");
            bool enableColor = false;
            if (hardwood_tint_enable != null)
            {
                if (hardwood_tint_enable.Type == AssetPropertyType.Integer)
                    enableColor = ((hardwood_tint_enable as AssetPropertyInteger).Value != 0);
                else if (hardwood_tint_enable.Type == AssetPropertyType.Boolean)
                    enableColor = (hardwood_tint_enable as AssetPropertyBoolean).Value;
            }

            if (enableColor)
            {
                AssetPropertyDoubleArray4d hardwood_tint_color = asset.FindByName("hardwood_tint_color") as AssetPropertyDoubleArray4d;
                if (null != hardwood_tint_color)
                {
                    IList<double> color = hardwood_tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                }
            }

            AssetPropertyBoolean common_Tint_toggle = asset.FindByName("common_Tint_toggle") as AssetPropertyBoolean;
            if (null != common_Tint_toggle && common_Tint_toggle.Value)
            {
                AssetPropertyDoubleArray4d common_Tint_color = asset.FindByName("common_Tint_color") as AssetPropertyDoubleArray4d;
                if (null != common_Tint_color)
                {
                    IList<double> c = common_Tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(c[0], c[1], c[2]);
                }
            }

            //其他
            AssetProperty hardwood_application = asset.FindByName("hardwood_application");
            int opt = -1;
            if (null != hardwood_application)
            {
                if (hardwood_application.Type == AssetPropertyType.Integer)
                    opt = (hardwood_application as AssetPropertyInteger).Value;
                else if (hardwood_application.Type == AssetPropertyType.Enumeration)
                    opt = (hardwood_application as AssetPropertyEnum).Value;
            }
            if (0 == opt)
                va3cMaterial.shininess = "20";
            else if (1 == opt)
                va3cMaterial.shininess = "10";
        }

        static private void GetMasonrycmuMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            //纹理贴图，颜色
            AssetPropertyDoubleArray4d masonrycmu_color = asset.FindByName("masonrycmu_color") as AssetPropertyDoubleArray4d;
            if (null != masonrycmu_color)
            {
                string textureFilePath = "";
                foreach (AssetProperty subProp in masonrycmu_color.GetAllConnectedProperties())
                {
                    if (subProp.Type == AssetPropertyType.Asset)
                    {
                        if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                        {
                            va3cMaterial.mapDiffuse = Path.GetFileName(textureFilePath);
                            break;
                        }
                    }
                }

                //masonrycmu材质只有在没有贴图的时候才有颜色
                if (string.IsNullOrEmpty(va3cMaterial.mapDiffuse))
                {
                    IList<double> color = masonrycmu_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                }
            }

            AssetPropertyBoolean common_Tint_toggle = asset.FindByName("common_Tint_toggle") as AssetPropertyBoolean;
            if (null != common_Tint_toggle && common_Tint_toggle.Value)
            {
                AssetPropertyDoubleArray4d common_Tint_color = asset.FindByName("common_Tint_color") as AssetPropertyDoubleArray4d;
                if (null != common_Tint_color)
                {
                    IList<double> color = common_Tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                }
            }

            //凹凸贴图
            AssetProperty masonrycmu_pattern_map = asset.FindByName("masonrycmu_pattern_map");
            if (null != masonrycmu_pattern_map)
            {
                string textureFilePath = "";
                foreach (AssetProperty subProp in masonrycmu_pattern_map.GetAllConnectedProperties())
                {
                    if (subProp.Type == AssetPropertyType.Asset)
                    {
                        if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                        {
                            va3cMaterial.mapBump = Path.GetFileName(textureFilePath);
                            break;
                        }
                    }
                }
            }

            AssetPropertyDouble masonrycmu_pattern_height_d = asset.FindByName("masonrycmu_pattern_height") as AssetPropertyDouble;
            if (null != masonrycmu_pattern_height_d)
                va3cMaterial.bumpScale = (masonrycmu_pattern_height_d.Value * BUMP_RADIO_HUNDRED).ToString();
            else
            {
                AssetPropertyFloat masonrycmu_pattern_height_f = asset.FindByName("masonrycmu_pattern_height") as AssetPropertyFloat;
                if (null != masonrycmu_pattern_height_f)
                    va3cMaterial.bumpScale = (masonrycmu_pattern_height_f.Value * BUMP_RADIO_HUNDRED).ToString();
            }
        }

        static private void GetMetalMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            va3cMaterial.isMetal = true;

            //纹理贴图
            AssetProperty metal_pattern = asset.FindByName("metal_pattern");
            if (null != metal_pattern)
            {
                int opt = -1;
                if (metal_pattern.Type == AssetPropertyType.Integer)
                    opt = (metal_pattern as AssetPropertyInteger).Value;
                else if (metal_pattern.Type == AssetPropertyType.Enumeration)
                    opt = (metal_pattern as AssetPropertyEnum).Value;

                if (opt == 4)
                {
                    AssetProperty metal_pattern_shader = asset.FindByName("metal_pattern_shader");
                    string textureFilePath = "";
                    foreach (AssetProperty subProp in metal_pattern_shader.GetAllConnectedProperties())
                    {
                        if (subProp.Type == AssetPropertyType.Asset)
                        {
                            if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                            {
                                va3cMaterial.mapBump = Path.GetFileName(textureFilePath);
                                break;
                            }
                        }
                    }
                }
            }

            //颜色
            AssetProperty metal_type = asset.FindByName("metal_type");
            if (null != metal_type)
            {
                int opt = -1;
                if (metal_type.Type == AssetPropertyType.Integer)
                    opt = (metal_type as AssetPropertyInteger).Value;
                else if (metal_type.Type == AssetPropertyType.Enumeration)
                    opt = (metal_type as AssetPropertyEnum).Value;

                switch (opt)
                {
                    case 0:
                        va3cMaterial.color = ColorToString(0.957, 0.957, 0.957).ToString();
                        break;
                    case 1:
                        //氧化铝为metalcolor
                        AssetPropertyDoubleArray4d metal_color = asset.FindByName("metal_color") as AssetPropertyDoubleArray4d;
                        if (null != metal_color)
                        {
                            IList<double> color = metal_color.GetValueAsDoubles();
                            va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                        }
                        break;
                    case 2:
                        va3cMaterial.color = ColorToString(0.957, 0.957, 0.957).ToString();
                        break;
                    case 3:
                        va3cMaterial.color = ColorToString(0.737, 0.314, 0.184).ToString();
                        break;
                    case 4:
                        va3cMaterial.color = ColorToString(0.796, 0.604, 0.231).ToString();
                        break;
                    case 5:
                        va3cMaterial.color = ColorToString(0.412, 0.302, 0.231).ToString();
                        break;
                    case 6:
                        va3cMaterial.color = ColorToString(0.745, 0.737, 0.729).ToString();
                        break;
                    case 7:
                        va3cMaterial.color = ColorToString(0.647, 0.678, 0.694).ToString();
                        break;
                    default:
                        break;
                }
            }

            AssetPropertyBoolean common_Tint_toggle = asset.FindByName("common_Tint_toggle") as AssetPropertyBoolean;
            if (null != common_Tint_toggle && common_Tint_toggle.Value)
            {
                AssetPropertyDoubleArray4d common_Tint_color = asset.FindByName("common_Tint_color") as AssetPropertyDoubleArray4d;
                if (null != common_Tint_color)
                {
                    IList<double> color = common_Tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                }
            }

            //其他
            AssetProperty metal_finish = asset.FindByName("metal_finish");
            if (null != metal_finish)
            {
                int opt = -1;
                if (metal_finish.Type == AssetPropertyType.Integer)
                    opt = (metal_finish as AssetPropertyInteger).Value;
                else if (metal_finish.Type == AssetPropertyType.Enumeration)
                    opt = (metal_finish as AssetPropertyEnum).Value;

                switch (opt)
                {
                    case 0:
                        va3cMaterial.shininess = "50";
                        break;
                    case 1:
                        va3cMaterial.shininess = "40";
                        break;
                    case 2:
                        va3cMaterial.shininess = "30";
                        break;
                    case 3:
                        va3cMaterial.shininess = "10";
                        break;
                    default:
                        break;
                }
            }
        }

        static private void GetMirrorMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            //颜色
            AssetPropertyDoubleArray4d mirror_tintcolor = asset.FindByName("mirror_tintcolor") as AssetPropertyDoubleArray4d;
            if (null != mirror_tintcolor)
            {
                IList<double> color = mirror_tintcolor.GetValueAsDoubles();
                va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
            }
            AssetPropertyBoolean common_Tint_toggle = asset.FindByName("common_Tint_toggle") as AssetPropertyBoolean;
            if (null != common_Tint_toggle && common_Tint_toggle.Value)
            {
                AssetPropertyDoubleArray4d common_Tint_color = asset.FindByName("common_Tint_color") as AssetPropertyDoubleArray4d;
                if (null != common_Tint_color)
                {
                    IList<double> color = common_Tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                }
            }

            va3cMaterial.reflectivity = "100";
        }

        static private void GetPlasticvinylMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            //浮雕贴图
            //Revit渲染中好像没显示这部分
            //AssetProperty plasticvinyl_pattern_map = asset.FindByName("plasticvinyl_pattern_map");
            //if (null != plasticvinyl_pattern_map)
            //{
            //    string textureFilePath = "";
            //    foreach (AssetProperty subProp in plasticvinyl_pattern_map.GetAllConnectedProperties())
            //    {
            //        if (subProp.Type == AssetPropertyType.Asset)
            //        {
            //            if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
            //            {
            //                va3cMaterial.mapDiffuse = Path.GetFileName(textureFilePath);
            //                break;
            //            }
            //        }
            //    }
            //}

            //凹凸贴图
            AssetProperty plasticvinyl_bump_map = asset.FindByName("plasticvinyl_bump_map");
            if (null != plasticvinyl_bump_map)
            {
                string textureFilePath = "";
                foreach (AssetProperty subProp in plasticvinyl_bump_map.GetAllConnectedProperties())
                {
                    if (subProp.Type == AssetPropertyType.Asset)
                    {
                        if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                        {
                            va3cMaterial.mapBump = Path.GetFileName(textureFilePath);
                            break;
                        }
                    }
                }
            }

            AssetPropertyDouble plasticvinyl_bump_amount_d = asset.FindByName("plasticvinyl_bump_amount") as AssetPropertyDouble;
            if (null != plasticvinyl_bump_amount_d)
                va3cMaterial.bumpScale = (plasticvinyl_bump_amount_d.Value * BUMP_RADIO_HUNDRED).ToString();
            else
            {
                AssetPropertyFloat plasticvinyl_bump_amount_f = asset.FindByName("plasticvinyl_bump_amount") as AssetPropertyFloat;
                if (null != plasticvinyl_bump_amount_f)
                    va3cMaterial.bumpScale = (plasticvinyl_bump_amount_f.Value * BUMP_RADIO_HUNDRED).ToString();
            }

            //颜色
            AssetPropertyDoubleArray4d plasticvinyl_color = asset.FindByName("plasticvinyl_color") as AssetPropertyDoubleArray4d;
            if (null != plasticvinyl_color)
            {
                IList<double> color = plasticvinyl_color.GetValueAsDoubles();
                va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
            }
            AssetPropertyBoolean common_Tint_toggle = asset.FindByName("common_Tint_toggle") as AssetPropertyBoolean;
            if (null != common_Tint_toggle && common_Tint_toggle.Value)
            {
                AssetPropertyDoubleArray4d common_Tint_color = asset.FindByName("common_Tint_color") as AssetPropertyDoubleArray4d;
                if (null != common_Tint_color)
                {
                    IList<double> color = common_Tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                }
            }

            //其他
            AssetPropertyInteger plasticvinyl_application_i = asset.FindByName("plasticvinyl_application") as AssetPropertyInteger;
            AssetPropertyEnum plasticvinyl_application_e = asset.FindByName("plasticvinyl_application") as AssetPropertyEnum;
            int opt = -1;
            if (plasticvinyl_application_i != null)
                opt = plasticvinyl_application_i.Value;
            else if (plasticvinyl_application_e != null)
                opt = plasticvinyl_application_e.Value;
            if (0 == opt)
                va3cMaterial.shininess = "20";
            else if (1 == opt)
                va3cMaterial.shininess = "10";
        }

        static private void GetSolidglassMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            //浮雕贴图
            AssetProperty solidglass_bump_enable = asset.FindByName("solidglass_bump_enable");
            if (null != solidglass_bump_enable)
            {
                int opt = -1;
                if (solidglass_bump_enable.Type == AssetPropertyType.Integer)
                    opt = (solidglass_bump_enable as AssetPropertyInteger).Value;
                else if (solidglass_bump_enable.Type == AssetPropertyType.Enumeration)
                    opt = (solidglass_bump_enable as AssetPropertyEnum).Value;

                if (opt == 3)   // 使用自定义图片
                {
                    AssetProperty solidglass_bump_map = asset.FindByName("solidglass_bump_map");
                    if (null != solidglass_bump_map)
                    {
                        string textureFilePath = "";
                        foreach (AssetProperty subProp in solidglass_bump_map.GetAllConnectedProperties())
                        {
                            if (subProp.Type == AssetPropertyType.Asset)
                            {
                                if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                                {
                                    va3cMaterial.mapBump = Path.GetFileName(textureFilePath);
                                    break;
                                }
                            }
                        }
                    }

                    AssetProperty solidglass_bump_amout = asset.FindByName("solidglass_bump_amount");
                    if (null != solidglass_bump_amout)
                    {
                        if (solidglass_bump_amout.Type == AssetPropertyType.Float)
                            va3cMaterial.bumpScale = ((solidglass_bump_amout as AssetPropertyFloat).Value * BUMP_RADIO_HUNDRED).ToString();
                        else if (solidglass_bump_amout.Type == AssetPropertyType.Double1)
                            va3cMaterial.bumpScale = ((solidglass_bump_amout as AssetPropertyDouble).Value * BUMP_RADIO_HUNDRED).ToString();
                    }
                }
            }

            //颜色
            AssetProperty solidglass_transmittance = asset.FindByName("solidglass_transmittance");
            if (solidglass_transmittance != null)
            {
                int opt = -1;
                if (solidglass_transmittance.Type == AssetPropertyType.Integer)
                    opt = (solidglass_transmittance as AssetPropertyInteger).Value;
                else if (solidglass_transmittance.Type == AssetPropertyType.Enumeration)
                    opt = (solidglass_transmittance as AssetPropertyEnum).Value;

                switch (opt)
                {
                    case 0:
                        va3cMaterial.color = ColorToString(0.858, 0.893, 0.879).ToString();
                        break;
                    case 1:
                        va3cMaterial.color = ColorToString(0.676, 0.797, 0.737).ToString();
                        break;
                    case 2:
                        va3cMaterial.color = ColorToString(0.451, 0.449, 0.472).ToString();
                        break;
                    case 3:
                        va3cMaterial.color = ColorToString(0.367, 0.514, 0.651).ToString();
                        break;
                    case 4:
                        va3cMaterial.color = ColorToString(0.654, 0.788, 0.772).ToString();
                        break;
                    case 5:
                        va3cMaterial.color = ColorToString(0.583, 0.516, 0.467).ToString();
                        break;
                    case 6:
                        AssetPropertyDoubleArray4d solidglass_transmittance_custom_color = asset.FindByName("solidglass_transmittance_custom_color") as AssetPropertyDoubleArray4d;
                        if (null != solidglass_transmittance_custom_color)
                        {
                            IList<double> color = solidglass_transmittance_custom_color.GetValueAsDoubles();
                            va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                        }
                        break;
                    default:
                        break;
                }
            }

            AssetPropertyBoolean common_Tint_toggle = asset.FindByName("common_Tint_toggle") as AssetPropertyBoolean;
            if (null != common_Tint_toggle && common_Tint_toggle.Value)
            {
                AssetPropertyDoubleArray4d common_Tint_color = asset.FindByName("common_Tint_color") as AssetPropertyDoubleArray4d;
                if (null != common_Tint_color)
                {
                    IList<double> color = common_Tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                }
            }

            //其他
            AssetProperty solidglass_reflectance = asset.FindByName("solidglass_reflectance");
            if (null != solidglass_reflectance)
            {
                if (solidglass_reflectance.Type == AssetPropertyType.Float)
                    va3cMaterial.opacity = (solidglass_reflectance as AssetPropertyFloat).Value.ToString();
                else if (solidglass_reflectance.Type == AssetPropertyType.Double1)
                    va3cMaterial.opacity = (solidglass_reflectance as AssetPropertyDouble).Value.ToString();
            }

            AssetProperty solidglass_glossiness = asset.FindByName("solidglass_glossiness");
            if (null != solidglass_glossiness)
            {
                if (solidglass_glossiness.Type == AssetPropertyType.Float)
                    va3cMaterial.roughness = (1.0f - (solidglass_glossiness as AssetPropertyFloat).Value).ToString();
                else if (solidglass_glossiness.Type == AssetPropertyType.Double1)
                    va3cMaterial.roughness = (1.0f - (solidglass_glossiness as AssetPropertyDouble).Value).ToString();
            }

            AssetProperty solidglass_refr_itor = asset.FindByName("solidglass_refr_ior");
            if (null != solidglass_refr_itor)
            {
                if (solidglass_refr_itor.Type == AssetPropertyType.Float)
                    va3cMaterial.refractionRatio = (solidglass_refr_itor as AssetPropertyFloat).ToString();
                else if (solidglass_refr_itor.Type == AssetPropertyType.Double1)
                    va3cMaterial.refractionRatio = (solidglass_refr_itor as AssetPropertyDouble).ToString();
            }
        }

        static private void GetStoneMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            //纹理贴图（stone没有颜色）
            AssetProperty stone_color = asset.FindByName("stone_color");
            if (null != stone_color)
            {
                string textureFilePath = "";
                foreach (AssetProperty subProp in stone_color.GetAllConnectedProperties())
                {
                    if (subProp.Type == AssetPropertyType.Asset)
                    {
                        if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                        {
                            va3cMaterial.mapDiffuse = Path.GetFileName(textureFilePath);
                            break;
                        }
                    }
                }
            }

            //浮雕贴图
            AssetProperty stone_pattern_map = asset.FindByName("stone_pattern_map");
            if (null != stone_pattern_map)
            {
                string textureFilePath = "";
                foreach (AssetProperty subProp in stone_pattern_map.GetAllConnectedProperties())
                {
                    if (subProp.Type == AssetPropertyType.Asset)
                    {
                        if (GetAssetBitmapPath(subProp as Asset, ref textureFilePath))
                        {
                            va3cMaterial.mapBump = Path.GetFileName(textureFilePath);
                            break;
                        }
                    }
                }
            }

            AssetPropertyDouble stone_bump_amount_d = asset.FindByName("stone_bump_amount") as AssetPropertyDouble;
            if (null != stone_bump_amount_d)
                va3cMaterial.bumpScale = (stone_bump_amount_d.Value * BUMP_RADIO_HUNDRED).ToString();
            else
            {
                AssetPropertyFloat stone_bump_amount_f = asset.FindByName("stone_bump_amount") as AssetPropertyFloat;
                if (null != stone_bump_amount_f)
                    va3cMaterial.bumpScale = (stone_bump_amount_f.Value * BUMP_RADIO_HUNDRED).ToString();
            }

            AssetPropertyInteger stone_application_i = asset.FindByName("stone_application") as AssetPropertyInteger;
            AssetPropertyEnum stone_application_e = asset.FindByName("stone_application") as AssetPropertyEnum;
            int opt = -1;
            if (stone_application_i != null)
                opt = stone_application_i.Value;
            else if (stone_application_e != null)
                opt = stone_application_e.Value;
            if (0 == opt)
                va3cMaterial.shininess = "20";
            else if (1 == opt)
                va3cMaterial.shininess = "10";
        }

        static private void GetWallPaintMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            //颜色
            AssetPropertyDoubleArray4d wallpaint_color = asset.FindByName("wallpaint_color") as AssetPropertyDoubleArray4d;
            if (wallpaint_color != null)
            {
                IList<double> color = wallpaint_color.GetValueAsDoubles();
                va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
            }

            AssetPropertyBoolean common_Tint_toggle = asset.FindByName("common_Tint_toggle") as AssetPropertyBoolean;
            if (null != common_Tint_toggle && common_Tint_toggle.Value)
            {
                AssetPropertyDoubleArray4d common_Tint_color = asset.FindByName("common_Tint_color") as AssetPropertyDoubleArray4d;
                if (null != common_Tint_color)
                {
                    IList<double> color = common_Tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                }
            }

            //其他
            AssetProperty wallpaint_finish = asset.FindByName("wallpaint_finish");
            if (null != wallpaint_finish)
            {
                int opt = -1;
                if (wallpaint_finish.Type == AssetPropertyType.Integer)
                    opt = (wallpaint_finish as AssetPropertyInteger).Value;
                else if (wallpaint_finish.Type == AssetPropertyType.Enumeration)
                    opt = (wallpaint_finish as AssetPropertyEnum).Value;

                if (4 == opt)
                    va3cMaterial.shininess = "10";
                else if (5 == opt)
                    va3cMaterial.shininess = "30";
            }
        }

        static private void GetWaterMaterialInfo(Asset asset, ref Va3cContainer.Va3cMaterial va3cMaterial)
        {
            //颜色
            AssetProperty water_type = asset.FindByName("water_type");
            int type = -1;
            if (water_type.Type == AssetPropertyType.Integer)
                type = (water_type as AssetPropertyInteger).Value;
            else if (water_type.Type == AssetPropertyType.Enumeration)
                type = (water_type as AssetPropertyEnum).Value;

            AssetProperty water_tint_enable = asset.FindByName("water_tint_enable");
            int tintEnable = -1;
            if (water_tint_enable.Type == AssetPropertyType.Integer)
                tintEnable = (water_tint_enable as AssetPropertyInteger).Value;
            else if (water_tint_enable.Type == AssetPropertyType.Enumeration)
                tintEnable = (water_tint_enable as AssetPropertyEnum).Value;

            if (type == 0)
                va3cMaterial.color = ColorToString(0.412, 0.949, 0.843);
            else if (type == 1)
            {
                AssetPropertyDoubleArray4d water_tint_color = asset.FindByName("water_tint_color") as AssetPropertyDoubleArray4d;
                if (null != water_tint_color)
                {
                    IList<double> color = water_tint_color.GetValueAsDoubles();
                    va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                }
            }
            else if (type > 1 && type < 5)
            {
                if (tintEnable == 0)
                    va3cMaterial.color = ColorToString(0.416, 0.953, 0.843);
                else if (tintEnable == 1)
                    va3cMaterial.color = ColorToString(0.675, 0.682, 0.557);
                else if (tintEnable == 2)
                    va3cMaterial.color = ColorToString(0.047, 0.145, 0.239);
                else if (tintEnable == 3)
                    va3cMaterial.color = ColorToString(0.047, 0.145, 0.239);
                else if (tintEnable == 4)
                    va3cMaterial.color = ColorToString(0.459, 0.431, 0.188);
                else if (tintEnable == 5)
                    va3cMaterial.color = ColorToString(0.141, 0.161, 0.067);
                else if (tintEnable == 6)
                    va3cMaterial.color = ColorToString(0.141, 0.161, 0.067);
                else if (tintEnable == 7)
                {
                    AssetPropertyDoubleArray4d water_tint_color = asset.FindByName("water_tint_color") as AssetPropertyDoubleArray4d;
                    if (null != water_tint_color)
                    {
                        IList<double> color = water_tint_color.GetValueAsDoubles();
                        va3cMaterial.color = ColorToString(color[0], color[1], color[2]);
                    }
                }
            }

            //反射率、透明度、纹理图等不知道怎么取到
            va3cMaterial.opacity = "0.1";
            va3cMaterial.reflectivity = "0.3";
        }
        
        static private string ColorToString(double r, double g, double b)
        {
            int red = (int)(r * 255.0 + 0.5);
            int green = (int)(g * 255.0 + 0.5);
            int blue = (int)(b * 255.0 + 0.5);
            int color = (red) << 16 | (green) << 8 | blue;

            return color.ToString();
        }

        #endregion
    }
}
