#if UNITY_EDITOR
using UnityEditor;

namespace Olam.EditorTools
{
    /// <summary>
    /// Импорт моделей Rodin, обработанных в Blender (Models/Rodin/Processed):
    /// масштаб 1:1 из FBX, без материалов из файла — их делает OlamRodinPrefabs.
    /// Нормали и карта металла/гладкости — в линейном пространстве; на Android ASTC.
    /// </summary>
    public class OlamRodinImport : AssetPostprocessor
    {
        public const string ProcessedRoot = "Assets/OLAM/Models/Rodin/Processed/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ProcessedRoot)) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.isReadable = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.generateSecondaryUV = false;
            importer.addCollider = false;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ProcessedRoot)) return;
            var importer = (TextureImporter)assetImporter;
            bool normal = assetPath.EndsWith("_Normal.png");
            bool data = assetPath.EndsWith("_MetallicSmoothness.png");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !(normal || data);
            importer.alphaSource = data ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;

            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
#endif
