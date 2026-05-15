using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public static class StarIndexBuilder
{
    private const string CsvName  = "allStars.csv";
    private const string IdxName  = "allStars_index.csv";
    private const string ResPath  = "Assets/Resources/" + CsvName;

    [MenuItem("Lost Stars/1. Move allStars to StreamingAssets")]
    public static void MoveCSV()
    {
        if (!File.Exists(ResPath))
        {
            EditorUtility.DisplayDialog("Move CSV",
                ResPath + " not found.\nMaybe it was already moved?", "OK");
            return;
        }

        Directory.CreateDirectory(Application.streamingAssetsPath);
        string dest = "Assets/StreamingAssets/" + CsvName;
        string err  = AssetDatabase.MoveAsset(ResPath, dest);
        if (!string.IsNullOrEmpty(err))
        {
            Debug.LogError("Move failed: " + err);
            return;
        }

        AssetDatabase.Refresh();
        Debug.Log("Moved " + CsvName + " → " + dest);
        EditorUtility.DisplayDialog("Done",
            CsvName + " moved to Assets/StreamingAssets/.\n\nNow run step 2 to build the index.", "OK");
    }

    [MenuItem("Lost Stars/2. Build Star Index")]
    public static void BuildIndex()
    {
        string csvPath = Path.Combine(Application.streamingAssetsPath, CsvName);
        string idxPath = Path.Combine(Application.streamingAssetsPath, IdxName);

        if (!File.Exists(csvPath))
        {
            EditorUtility.DisplayDialog("Build Star Index",
                CsvName + " not found in StreamingAssets.\nRun step 1 first.", "OK");
            return;
        }

        EditorUtility.DisplayProgressBar("Building Star Index", "Reading " + CsvName + "...", 0f);

        try
        {
            byte[] bytes = File.ReadAllBytes(csvPath);
            int   total  = bytes.Length;
            var   entries = new List<(string name, int start, int length)>();

            bool headerSkipped = false;
            string currentCluster = null;
            int    clusterStart   = 0;
            int    pos            = 0;

            while (pos <= total)
            {
                int lineStart = pos;

                // Advance to end of line
                while (pos < total && bytes[pos] != '\n') pos++;

                int lineEnd = pos; // index of '\n' (or end of file)

                // Trim trailing \r
                int lineLen = lineEnd - lineStart;
                if (lineLen > 0 && bytes[lineStart + lineLen - 1] == '\r') lineLen--;

                if (lineLen > 0)
                {
                    // Find the first comma to extract column 0 cheaply
                    int commaPos = lineStart;
                    while (commaPos < lineStart + lineLen && bytes[commaPos] != ',') commaPos++;

                    if (commaPos < lineStart + lineLen)
                    {
                        string col0 = Encoding.UTF8.GetString(bytes, lineStart, commaPos - lineStart).Trim();

                        if (!headerSkipped)
                        {
                            headerSkipped = true; // first line is the header
                        }
                        else if (!string.IsNullOrEmpty(col0) && col0 != currentCluster)
                        {
                            if (currentCluster != null)
                                entries.Add((currentCluster, clusterStart, lineStart - clusterStart));

                            currentCluster = col0;
                            clusterStart   = lineStart;
                        }
                    }
                }

                if (pos >= total) break;
                pos++; // skip '\n'
            }

            // Flush the last cluster
            if (currentCluster != null)
                entries.Add((currentCluster, clusterStart, total - clusterStart));

            // Write index file
            Directory.CreateDirectory(Application.streamingAssetsPath);
            var sb = new StringBuilder();
            sb.AppendLine("Cluster_Name,StartByte,ByteLength");
            foreach (var (name, start, length) in entries)
                sb.AppendLine($"{name},{start},{length}");
            File.WriteAllText(idxPath, sb.ToString(), Encoding.UTF8);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Done",
                $"Index built: {entries.Count} clusters.\nSaved to StreamingAssets/{IdxName}", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
