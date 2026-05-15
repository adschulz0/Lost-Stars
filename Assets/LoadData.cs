using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class LoadData : MonoBehaviour
{
    public TextAsset clustersFile;
    public TextAsset starsFile;
    public TextAsset singleStarFile;
    public String singleStarName;

    //RV,pm_ra,pm_dec,l,n
    // Dictionaries to store star data by cluster name
    private Dictionary<string, List<float>> raByCluster = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> decByCluster = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> helioDistByCluster = new Dictionary<string, List<float>>();
    public Dictionary<string, List<float>> xByCluster = new Dictionary<string, List<float>>();
    public Dictionary<string, List<float>> yByCluster = new Dictionary<string, List<float>>();
    public Dictionary<string, List<float>> zByCluster = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> rvByCluster = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> pm_raByCluster = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> pm_decByCluster = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> lByCluster = new Dictionary<string, List<float>>();
    private Dictionary<string, List<float>> bByCluster = new Dictionary<string, List<float>>();

    // Lists to store cluster positions
    public Dictionary<string, Vector3> clusterPositions = new Dictionary<string, Vector3>();

    void Start()
    {
        ReadStarsCSV();
        //ReadSingleStarCSV();

        ReadClustersCSV();


    }

    void ReadSingleStarCSV()
    {
        TextAsset csvFile = singleStarFile;
        if (csvFile == null)
        {
            Debug.LogError("Stars CSV file not found");
            return;
        }

        // Split the CSV file into lines
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

        // Initialize dictionaries to hold the values
        foreach (string line in lines)
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Split each line into columns
            string[] columns = line.Split(',');

            // Check if the line is a header line
            if (columns.Length > 1 && columns[1].Trim().Equals("RA", StringComparison.OrdinalIgnoreCase))
            {
                // This is a header line, skip it
                continue;
            }
            //if starts with ra instead of cluster name
            /*float ra = float.Parse(columns[0]);
            float dec = float.Parse(columns[1]);
            float x = float.Parse(columns[2]);
            float y = float.Parse(columns[3]);
            float z = float.Parse(columns[4]);
            float rv = float.Parse(columns[5]);
            float pm_ra = float.Parse(columns[6]);
            float pm_dec = float.Parse(columns[7]);
            float l = float.Parse(columns[8]);
            float b = float.Parse(columns[9]);*/

            //if starts with cluster name
            float ra = float.Parse(columns[1]);
            float dec = float.Parse(columns[2]);
            float helio_dist = float.Parse(columns[3]);
            float x = float.Parse(columns[4]);
            float y = float.Parse(columns[5]);
            float z = float.Parse(columns[6]);
            float rv = float.Parse(columns[7]);
            float pm_ra = float.Parse(columns[8]);
            float pm_dec = float.Parse(columns[9]);
            float l = float.Parse(columns[10]);
            float b = float.Parse(columns[11]);

            //if no cluster name and has helio dist
            /*float ra = float.Parse(columns[0]);
            float dec = float.Parse(columns[1]);
            float helio_dist = float.Parse(columns[2]);
            float x = float.Parse(columns[3]);
            float y = float.Parse(columns[4]);
            float z = float.Parse(columns[5]);
            float rv = float.Parse(columns[6]);
            float pm_ra = float.Parse(columns[7]);
            float pm_dec = float.Parse(columns[8]);*/

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

    void ReadStarsCSV()
    {
        TextAsset csvFile = starsFile;
        if (csvFile == null)
        {
            Debug.LogError("Stars CSV file not found");
            return;
        }

        // Split the CSV file into lines
        string[] lines = csvFile.text.Split(new char[] { '\n' });

        // Initialize dictionaries to hold the values
        foreach (string line in lines)
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Split each line into columns
            string[] columns = line.Split(',');

            // Check if the line is a header line
            if (columns.Length > 1 && columns[1].Trim().Equals("RA", StringComparison.OrdinalIgnoreCase))
            {
                // This is a header line, skip it
                continue;
            }

            string clusterName = columns[0].Trim();
            //Debug.Log(columns[1]);
            float ra = float.Parse(columns[1]);
            float dec = float.Parse(columns[2]);
            float helioDist = float.Parse(columns[3]);
            float x = float.Parse(columns[4]);
            float y = float.Parse(columns[5]);
            float z = float.Parse(columns[6]);
            float rv = float.Parse(columns[7]);
            float pm_ra = float.Parse(columns[8]);
            float pm_dec = float.Parse(columns[9]);
            float l = float.Parse(columns[10]);
            float b = float.Parse(columns[11]);

            // Add values to dictionaries indexed by cluster name
            if (!raByCluster.ContainsKey(clusterName))
            {
                raByCluster.Add(clusterName, new List<float>());
                decByCluster.Add(clusterName, new List<float>());
                helioDistByCluster.Add(clusterName, new List<float>());
                xByCluster.Add(clusterName, new List<float>());
                yByCluster.Add(clusterName, new List<float>());
                zByCluster.Add(clusterName, new List<float>());
                rvByCluster.Add(clusterName, new List<float>());
                pm_raByCluster.Add(clusterName, new List<float>());
                pm_decByCluster.Add(clusterName, new List<float>());
                lByCluster.Add(clusterName, new List<float>());
                bByCluster.Add(clusterName, new List<float>());
            }

            raByCluster[clusterName].Add(ra);
            decByCluster[clusterName].Add(dec);
            helioDistByCluster[clusterName].Add(helioDist);
            xByCluster[clusterName].Add(x);
            yByCluster[clusterName].Add(y);
            zByCluster[clusterName].Add(z);
            rvByCluster[clusterName].Add(rv);
            pm_raByCluster[clusterName].Add(pm_ra);
            pm_decByCluster[clusterName].Add(pm_dec);
            lByCluster[clusterName].Add(l);
            bByCluster[clusterName].Add(b);
        }

        // Optionally convert lists to arrays if needed
        // Example: ra = raByCluster["ClusterName"].ToArray();
    }

    void ReadClustersCSV()
    {
        if (clustersFile == null)
        {
            Debug.LogError("Clusters CSV file not assigned");
            return;
        }

        TextAsset csvFile = clustersFile;

        // Split the CSV file into lines
        string[] lines = csvFile.text.Split(new char[] { '\n' });

        // Process each line (assuming headers are handled)
        foreach (string line in lines)
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Split each line into columns
            string[] columns = line.Split(',');

            // Check if the line is a header line
            if (columns.Length > 1 && columns[1].Trim().Equals("RA", StringComparison.OrdinalIgnoreCase))
            {
                // This is a header line, skip it
                continue;
            }

            string clusterName = columns[0].Trim();

            //indexes 14, 15, 16 are x, y, z
            float posX = float.Parse(columns[14]);
            float posY = float.Parse(columns[15]);
            float posZ = float.Parse(columns[16]);

            // Store cluster positions as Vector3
            Vector3 position = new Vector3(posX, posY, posZ);
            clusterPositions.Add(clusterName, position);
        }
    }

}
