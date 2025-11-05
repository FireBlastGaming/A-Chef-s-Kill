using System.IO;
using UnityEditor;
using UnityEngine;

public static class DevAI
{
    public class SaveDetector : AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var p in paths)
            {
                if (p.EndsWith(".unity") || p.EndsWith(".prefab"))
                    EmitSnapshot(p);
            }
            return paths;
        }
    }

    static void EmitSnapshot(string assetPath)
    {
        var info = new
        {
            kind = "unity-save",
            ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            user = System.Environment.UserName,
            type = assetPath.EndsWith(".unity") ? "scene" : "prefab",
            path = assetPath.Replace("\\", "/")
        };

        var projectRoot = Application.dataPath + "/..";
        var inboxDir = Path.Combine(projectRoot, ".devai/inbox");
        Directory.CreateDirectory(inboxDir);

        // one-line json for the inbox
        File.AppendAllText(Path.Combine(inboxDir, "save-events.jsonl"),
                           JsonUtility.ToJson(info) + "\n");

        // optional: also drop a per-save file (handy for debugging)
        var rtDir = Path.Combine(projectRoot, ".devai/runtime");
        Directory.CreateDirectory(rtDir);
        File.WriteAllText(Path.Combine(rtDir,
            $"save-{Path.GetFileName(assetPath)}-{info.ts}.json"),
            JsonUtility.ToJson(info));
    }
}
