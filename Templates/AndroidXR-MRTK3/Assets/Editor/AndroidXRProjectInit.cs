using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// テンプレートから作ったプロジェクトの初期化。パッケージ名・製品名・会社名を設定する。
/// テンプレートのままだと全プロジェクトが同じパッケージ名になり、実機で上書きインストールされるため。
/// 基本版では、手のゴーストに使う XR Hands の HandVisualizer サンプルも取り込む。
///
/// unity run &lt;project&gt; -- -buildTarget Android -executeMethod AndroidXRProjectInit.Run -appId com.example.myxrapp -productName MyXrApp [-companyName MyCompany]
/// パッケージ名は Android・Standalone・iOS をそろえる（使うのは Android だけだが、ほかがテンプレートの元の名前のまま残らないように）。
/// </summary>
public static class AndroidXRProjectInit
{
    public static void Run()
    {
        var appId = GetArg("-appId");
        var productName = GetArg("-productName");
        var companyName = GetArg("-companyName");

        if (string.IsNullOrEmpty(appId))
        {
            Debug.LogError("[AndroidXRProjectInit] -appId が指定されていません（例: -appId com.example.myxrapp）");
            EditorApplication.Exit(1);
            return;
        }

        foreach (var target in new[] { NamedBuildTarget.Android, NamedBuildTarget.Standalone, NamedBuildTarget.iOS })
            PlayerSettings.SetApplicationIdentifier(target, appId);
        Debug.Log($"[AndroidXRProjectInit] Package Name -> {appId}");

        if (!string.IsNullOrEmpty(productName))
        {
            PlayerSettings.productName = productName;
            Debug.Log($"[AndroidXRProjectInit] Product Name -> {productName}");
        }

        if (!string.IsNullOrEmpty(companyName))
        {
            PlayerSettings.companyName = companyName;
            Debug.Log($"[AndroidXRProjectInit] Company Name -> {companyName}");
        }

        // 基本版: 手のゴーストに使う XR Hands のサンプルを取り込む（テンプレートには同梱していない）
        AndroidXRHandVisualizer.ImportIfNeeded();

        AssetDatabase.SaveAssets();
    }

    static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        }
        return null;
    }
}
