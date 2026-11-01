using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 手のゴースト表示に使う XR Hands の HandVisualizer サンプルを Package Manager から取り込む。
/// テンプレートにはサンプルを同梱していない（シーンは GUID で参照しているので、取り込むと自動でつながる）。
/// 取り込んだマテリアルは Built-in 用なので、URP のシェーダーに切り替えて保存する（しないと手がピンクになる）。
///
/// Imports the XR Hands "HandVisualizer" sample (used for the hand visuals). The templates do not ship the sample;
/// the scenes reference its prefabs by GUID, so they reconnect after the import.
/// Menu: Tools > Android XR > Import Hand Visualizer Sample. AndroidXRProjectInit.Run also calls this.
/// </summary>
public static class AndroidXRHandVisualizer
{
    const string Package = "com.unity.xr.hands";
    const string SampleName = "HandVisualizer";

    [MenuItem("Tools/Android XR/Import Hand Visualizer Sample")]
    static void ImportFromMenu() => Import(force: true);

    /// <summary>基本版のシーン（手のゴーストを使う）があるときだけ取り込む。</summary>
    public static void ImportIfNeeded()
    {
        if (AssetDatabase.LoadMainAssetAtPath("Assets/Scenes/VR_SampleScene.unity") == null) return;
        Import(force: false);
    }

    static void Import(bool force)
    {
        var sample = Sample.FindByPackage(Package, null).FirstOrDefault(s => s.displayName == SampleName);
        if (string.IsNullOrEmpty(sample.displayName))
        {
            Debug.LogWarning($"[AndroidXRHandVisualizer] {Package} の {SampleName} サンプルが見つかりません");
            return;
        }
        if (!sample.isImported || force)
        {
            sample.Import(Sample.ImportOptions.HideImportWindow | Sample.ImportOptions.OverridePreviousImports);
            AssetDatabase.Refresh();
            Debug.Log($"[AndroidXRHandVisualizer] Imported: {sample.importPath}");
        }
        ApplyPipelineShaders(sample.importPath);
    }

    // サンプルの MaterialPipelineHandler.asset に書かれた「マテリアル → URP のシェーダー名」に従って切り替える
    static void ApplyPipelineShaders(string importPath)
    {
        if (GraphicsSettings.currentRenderPipeline == null) return;
        var projectRoot = Path.GetDirectoryName(Application.dataPath);
        var rel = Path.GetRelativePath(projectRoot, importPath).Replace('\\', '/');
        var handler = Directory.GetFiles(importPath, "MaterialPipelineHandler.asset", SearchOption.AllDirectories).FirstOrDefault();
        if (handler == null) return;

        var pattern = new Regex(@"material: \{fileID: \d+, guid: (\w+), type: \d\}\s+useSRPShaderName: \d\s+scriptableRenderPipelineShaderName: (.+)");
        foreach (Match m in pattern.Matches(File.ReadAllText(handler)))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value));
            var shader = Shader.Find(m.Groups[2].Value.Trim());
            if (material == null || shader == null || material.shader == shader) continue;
            var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            Debug.Log($"[AndroidXRHandVisualizer] {material.name} -> {shader.name} ({rel})");
        }
    }
}
