using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

/// <summary>
/// 生成された Gradle プロジェクトの AndroidManifest から重複要素を畳む。
///
/// com.unity.xr.androidxr-openxr と com.google.xr.extensions を同居させると、
/// `uses-feature android.software.xr.api.spatial` が version=1 と version=3 の 2 行になり、
/// 属性が一致しないためマニフェストマージャが落ちる（Duplicate / Validation failed）。
///  - com.unity.xr.androidxr-openxr の SpatialAPIFeaturesConfigurator: 有効な拡張から最小版を算出（既定構成では 1）
///  - com.google.xr.extensions の XRSessionFeatureBuildHooks: Target API Version が Automatic のとき enum 末尾（3）
///
/// どちらも「同じことを 2 回言っている」だけなので、**最後に畳めば意味は変わらない**。
/// uses-feature の version が食い違う場合は**高い方を採用**する（低い方は上位互換で満たされる）。
///
/// callbackOrder を大きくして、各パッケージの後処理より後に走らせる。
/// </summary>
public class ManifestDedupe : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 9999;

    // 名前で一意にすべき要素。いずれも android:name がキー。
    static readonly string[] DedupeElements = { "uses-permission", "uses-feature", "uses-native-library" };

    const string AndroidNs = "http://schemas.android.com/apk/res/android";

    public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
    {
        // unityLibraryPath = .../Gradle/unityLibrary
        var targets = new[]
        {
            Path.Combine(unityLibraryPath, "src", "main", "AndroidManifest.xml"),
            Path.Combine(unityLibraryPath, "xrmanifest.androidlib", "AndroidManifest.xml"),
        };

        foreach (var t in targets)
        {
            try { Dedupe(t); }
            catch (Exception e) { Debug.LogError($"[ManifestDedupe] {t}: {e.GetType().Name}: {e.Message}"); }
        }
    }

    static void Dedupe(string path)
    {
        if (!File.Exists(path))
        {
            Debug.Log($"[ManifestDedupe] skip (無い): {path}");
            return;
        }

        var doc = new XmlDocument();
        doc.Load(path);
        var root = doc.DocumentElement;
        if (root == null) return;

        var removed = 0;
        foreach (var elementName in DedupeElements)
        {
            // manifest 直下と application 直下の両方に出る（uses-native-library は application 側）
            foreach (XmlElement parent in Parents(root))
            {
                var byName = new Dictionary<string, XmlElement>(StringComparer.Ordinal);
                foreach (var el in parent.ChildNodes.OfType<XmlElement>()
                             .Where(e => e.Name == elementName).ToList())
                {
                    var name = el.GetAttribute("name", AndroidNs);
                    if (string.IsNullOrEmpty(name)) continue;

                    if (!byName.TryGetValue(name, out var kept))
                    {
                        byName[name] = el;
                        continue;
                    }

                    // version が食い違うなら高い方を残す
                    var keptV = ParseVersion(kept.GetAttribute("version", AndroidNs));
                    var thisV = ParseVersion(el.GetAttribute("version", AndroidNs));
                    if (thisV > keptV)
                    {
                        parent.RemoveChild(kept);
                        byName[name] = el;
                        Debug.Log($"[ManifestDedupe] {elementName} '{name}': version {keptV} を捨てて {thisV} を採用");
                    }
                    else
                    {
                        parent.RemoveChild(el);
                        if (thisV != keptV)
                            Debug.Log($"[ManifestDedupe] {elementName} '{name}': version {thisV} を捨てて {keptV} を採用");
                        else
                            Debug.Log($"[ManifestDedupe] {elementName} '{name}': 完全重複を 1 つ削除");
                    }
                    removed++;
                }
            }
        }

        if (removed > 0)
        {
            doc.Save(path);
            Debug.Log($"[ManifestDedupe] {removed} 要素を削除: {path}");
        }
        else
        {
            Debug.Log($"[ManifestDedupe] 重複なし: {path}");
        }
    }

    static IEnumerable<XmlElement> Parents(XmlElement root)
    {
        yield return root;
        foreach (var app in root.ChildNodes.OfType<XmlElement>().Where(e => e.Name == "application"))
            yield return app;
    }

    /// <summary>android:version は 10 進 ("3") と 16 進 ("0x00010001") の両方が来る。</summary>
    static long ParseVersion(string s)
    {
        if (string.IsNullOrEmpty(s)) return -1;
        s = s.Trim();
        try
        {
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Convert.ToInt64(s.Substring(2), 16);
            return long.Parse(s, CultureInfo.InvariantCulture);
        }
        catch { return -1; }
    }
}
