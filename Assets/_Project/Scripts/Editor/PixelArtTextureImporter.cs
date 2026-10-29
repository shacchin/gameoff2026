using UnityEditor;
using UnityEngine;

// Assets/_Project/Art 以下の画像を、ピクセルアート向けの設定でインポートする
public class PixelArtTextureImporter : AssetPostprocessor
{
    const string ArtFolder = "Assets/_Project/Art/";
    const int PixelsPerUnit = 16;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ArtFolder)) return;

        // 初回インポート時だけ適用する（あとからインスペクターで変えた設定は上書きしない）
        if (!assetImporter.importSettingsMissing) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
    }
}
