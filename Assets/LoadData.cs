using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;

public class LoadData : MonoBehaviour
{
    public TextAsset clustersFile;
    public TextAsset singleStarFile;
    public String singleStarName;

    private Dictionary<string, List<float>> raByCluster          = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> decByCluster         = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> helioDistByCluster   = new Dictionary<string, List<float>>();
    public  Dictionary<string, List<float>> xByCluster           = new Dictionary<string, List<float>>();
    public  Dictionary<string, List<float>> yByCluster           = new Dictionary<string, List<float>>();
    public  Dictionary<string, List<float>> zByCluster           = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> rvByCluster          = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> pm_raByCluster       = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> pm_decByCluster      = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> lByCluster           = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> bByCluster           = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> releaseTimeByCluster = new Dictionary<string, List<float>>();

    public Dictionary<string, Vector3> clusterPositions          = new Dictionary<string, Vector3>();
    public Dictionary<string, float>   clusterRsun               = new Dictionary<string, float>();
    public Dictionary<string, float>   clusterRGC                = new Dictionary<string, float>();
    public Dictionary<string, float>   clusterRV                 = new Dictionary<string, float>();
    public Dictionary<string, float>   clusterMass               = new Dictionary<string, float>();
    public Dictionary<string, float>   clusterAge                = new Dictionary<string, float>();

    // Byte-range index into StreamingAssets/allStars.csv
    private string starsCsvPath;
    private Dictionary<string, (long start, long length)> starIndex = new Dictionary<string, (long, long)>();

    void Start()
    {
        //ReadSingleStarCSV();
        LoadStarIndex();
        ReadClustersCSV();
    }

    // -------------------------------------------------------------------------
    // Index loading (runs at startup — reads only the tiny index file)
    // -------------------------------------------------------------------------

    void LoadStarIndex()
    {
        starsCsvPath = Path.Combine(Application.streamingAssetsPath, "allStars.csv");
        string indexPath = Path.Combine(Application.streamingAssetsPath, "allStars_index.csv");

        if (!File.Exists(indexPath))
        {
            Debug.LogError("[LoadData] allStars_index.csv not found. " +
                           "Use 'Lost Stars > 1. Move allStars to StreamingAssets' " +
                           "then 'Lost Stars > 2. Build Star Index' from the Unity menu bar.");
            return;
        }

        string[] lines = File.ReadAllLines(indexPath, Encoding.UTF8);
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] cols = line.Split(',');
            if (cols.Length < 3) continue;
            if (cols[1].Trim().Equals("StartByte", StringComparison.OrdinalIgnoreCase)) continue;

            string name   = cols[0].Trim();
            long   start  = long.Parse(cols[1].Trim());
            long   length = long.Parse(cols[2].Trim());
            starIndex[name] = (start, length);
        }

        Debug.Log($"[LoadData] Star index loaded: {starIndex.Count} clusters indexed.");
    }

    public bool HasStarData(string clusterName) => starIndex.ContainsKey(clusterName);

    // -------------------------------------------------------------------------
    // On-demand star loading (called the first time a cluster is clicked)
    // -------------------------------------------------------------------------

    public void LoadStarsForCluster(string clusterName)
    {
        if (xByCluster.ContainsKey(clusterName)) return; // already loaded

        if (!starIndex.TryGetValue(clusterName, out var range))
        {
            Debug.LogError($"[LoadData] No index entry for cluster '{clusterName}'. " +
                           "Rebuild the star index via the Lost Stars menu.");
            return;
        }

        // Read only this cluster's byte range from the file
        byte[] buf = new byte[(int)range.length];
        using (var fs = new FileStream(starsCsvPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            fs.Seek(range.start, SeekOrigin.Begin);
            int totalRead = 0;
            while (totalRead < buf.Length)
            {
                int n = fs.Read(buf, totalRead, buf.Length - totalRead);
                if (n == 0) break;
                totalRead += n;
            }
        }

        var xList    = new List<float>();
        var yList    = new List<float>();
        var zList    = new List<float>();
        var raList   = new List<float>();
        var decList  = new List<float>();
        var helioList = new List<float>();
        var rvList   = new List<float>();
        var pmRaList = new List<float>();
        var pmDecList = new List<float>();
        var lList    = new List<float>();
        var bList    = new List<float>();
        var rtList   = new List<float>();

        string[] lines = Encoding.UTF8.GetString(buf).Split('\n');
        foreach (string line in lines)
        {
            string row = line.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(row)) continue;

            string[] cols = row.Split(',');
            if (cols.Length < 13) continue;

            raList.Add(float.Parse(cols[1]));
            decList.Add(float.Parse(cols[2]));
            helioList.Add(float.Parse(cols[3]));
            xList.Add(float.Parse(cols[4]));
            yList.Add(float.Parse(cols[5]));
            zList.Add(float.Parse(cols[6]));
            rvList.Add(float.Parse(cols[7]));
            pmRaList.Add(float.Parse(cols[8]));
            pmDecList.Add(float.Parse(cols[9]));
            lList.Add(float.Parse(cols[10]));
            bList.Add(float.Parse(cols[11]));
            rtList.Add(float.Parse(cols[12]));
        }

        xByCluster[clusterName]          = xList;
        yByCluster[clusterName]          = yList;
        zByCluster[clusterName]          = zList;
        raByCluster[clusterName]         = raList;
        decByCluster[clusterName]        = decList;
        helioDistByCluster[clusterName]  = helioList;
        rvByCluster[clusterName]         = rvList;
        pm_raByCluster[clusterName]      = pmRaList;
        pm_decByCluster[clusterName]     = pmDecList;
        lByCluster[clusterName]          = lList;
        bByCluster[clusterName]          = bList;
        releaseTimeByCluster[clusterName] = rtList;
    }

    // -------------------------------------------------------------------------
    // Column data accessor for StarColorMapper
    // -------------------------------------------------------------------------

    public List<float> GetColumnData(string clusterName, string column)
    {
        switch (column)
        {
            case "RA":           return raByCluster.TryGetValue(clusterName,        out var ra)   ? ra   : null;
            case "Dec":          return decByCluster.TryGetValue(clusterName,       out var dec)  ? dec  : null;
            case "Helio_Dist":   return helioDistByCluster.TryGetValue(clusterName, out var hd)   ? hd   : null;
            case "RV":           return rvByCluster.TryGetValue(clusterName,        out var rv)   ? rv   : null;
            case "pm_ra":        return pm_raByCluster.TryGetValue(clusterName,     out var pmra) ? pmra : null;
            case "pm_dec":       return pm_decByCluster.TryGetValue(clusterName,    out var pmdc) ? pmdc : null;
            case "l":            return lByCluster.TryGetValue(clusterName,         out var l)    ? l    : null;
            case "b":            return bByCluster.TryGetValue(clusterName,         out var b)    ? b    : null;
            case "Release_Time": return releaseTimeByCluster.TryGetValue(clusterName, out var rt) ? rt   : null;
            default:             return null;
        }
    }

    // -------------------------------------------------------------------------
    // Single-star CSV (kept for future use, currently commented out in Start)
    // -------------------------------------------------------------------------

    void ReadSingleStarCSV()
    {
        TextAsset csvFile = singleStarFile;
        if (csvFile == null)
        {
            Debug.LogError("Stars CSV file not found");
            return;
        }

        string[] lines = csvFile.text.Split(new char[] { '\n' });

        raByCluster.Add(singleStarName, new List<float>());
        decByCluster.Add(singleStarName, new List<float>());
        xByCluster.Add(singleStarName, new List<float>());
        yByCluster.Add(singleStarName, new List<float>());
        zByCluster.Add(singleStarName, new List<float>());
        rvByCluster.Add(singleStarName, new List<float>());
        pm_raByCluster.Add(singleStarName, new List<float>());
        pm_decByCluster.Add(singleStarName, new List<float>());
        lByCluster.Add(singleStarName, new List<float>());
        bByCluster.Add(singleStarName, new List<float>());

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] columns = line.Split(',');

            if (columns.Length > 1 && columns[1].Trim().Equals("RA", StringComparison.OrdinalIgnoreCase))
                continue;

            float ra       = float.Parse(columns[1]);
            float dec      = float.Parse(columns[2]);
            float helio_dist = float.Parse(columns[3]);
            float x        = float.Parse(columns[4]);
            float y        = float.Parse(columns[5]);
            float z        = float.Parse(columns[6]);
            float rv       = float.Parse(columns[7]);
            float pm_ra    = float.Parse(columns[8]);
            float pm_dec   = float.Parse(columns[9]);
            float l        = float.Parse(columns[10]);
            float b        = float.Parse(columns[11]);

            raByCluster[singleStarName].Add(ra);
            decByCluster[singleStarName].Add(dec);
            xByCluster[singleStarName].Add(x);
            yByCluster[singleStarName].Add(y);
            zByCluster[singleStarName].Add(z);
            rvByCluster[singleStarName].Add(rv);
            pm_raByCluster[singleStarName].Add(pm_ra);
            pm_decByCluster[singleStarName].Add(pm_dec);
        }
    }

    // -------------------------------------------------------------------------
    // Cluster positions (small file — loaded in full at startup, unchanged)
    // -------------------------------------------------------------------------

    void ReadClustersCSV()
    {
        if (clustersFile == null)
        {
            Debug.LogError("Clusters CSV file not assigned");
            return;
        }

        TextAsset csvFile = clustersFile;

        string[] lines = csvFile.text.Split(new char[] { '\n' });

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] columns = line.Split(',');

            if (columns.Length > 1 && columns[1].Trim().Equals("RA", StringComparison.OrdinalIgnoreCase))
                continue;

            if (columns.Length < 31) continue; // need up to col 30 (Age)

            string clusterName = columns[0].Trim();

            float posX = float.Parse(columns[14]);
            float posY = float.Parse(columns[15]);
            float posZ = float.Parse(columns[16]);

            clusterPositions.Add(clusterName, new Vector3(posX, posY, posZ));

            clusterRsun[clusterName] = float.Parse(columns[5]);
            clusterRGC[clusterName]  = float.Parse(columns[7]);
            clusterRV[clusterName]   = float.Parse(columns[8]);
            clusterMass[clusterName] = float.Parse(columns[19]);
            clusterAge[clusterName]  = float.Parse(columns[30]);
        }
    }
}
