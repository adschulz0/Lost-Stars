using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarPlotter : MonoBehaviour
{
    public float scale = 100.0f; // Scale factor for positioning
    public float starSize = 2f;


    public LoadData loadData; // Reference to the LoadData script

    public GameObject starPrefab; // Reference to the star prefab
    public GameObject clusterPrefab;
    public GameObject sunPrefab;
    public GameObject galaxyCenterPrefab;

    public float[] sunPos = new float[3];

    public int numToPlot;

    public StarColorMapper colorMapper;

    void Start()
    {
        // Ensure references are correctly assigned
        if (starPrefab == null)
        {
            Debug.LogError("Star Prefab is not assigned!");
            return;
        }

        if (loadData == null)
        {
            Debug.LogError("LoadData script is not assigned!");
            return;
        }

        InstantClusters();
        InstantGalaxyCenter();
        InstantSun(sunPos);
    }

    public void PlotStarsForCluster(string clusterName)
    {
        loadData.LoadStarsForCluster(clusterName);

        if (!loadData.xByCluster.ContainsKey(clusterName))
        {
            Debug.LogError($"No star data found for cluster {clusterName}");
            return;
        }

        List<float> xArray = loadData.xByCluster[clusterName];
        List<float> yArray = loadData.yByCluster[clusterName];
        List<float> zArray = loadData.zByCluster[clusterName];

        if (xArray.Count != yArray.Count || xArray.Count != zArray.Count)
        {
            Debug.LogError($"Star arrays for cluster {clusterName} have different lengths!");
            return;
        }

        for (int j = 0; j < xArray.Count; j++)
        {
            Vector3 position = new Vector3(xArray[j], yArray[j], zArray[j]);
            GameObject star = Instantiate(starPrefab, position * scale, Quaternion.identity);
            star.transform.localScale *= starSize;
            star.transform.SetParent(transform.Find(clusterName));
        }

        colorMapper?.ColorizeCluster(clusterName);
    }

    void InstantClusters()
    {
        // Ensure references are correctly assigned
        if (loadData == null)
        {
            Debug.LogError("LoadData script reference not set!");
            return;
        }

        List<string> clusterNames = new List<string>(loadData.clusterPositions.Keys);

        numToPlot = Mathf.Min(numToPlot, clusterNames.Count);

        for (int i = 0; i < numToPlot; i++)
        {
            string clusterName = clusterNames[i];

            if (!loadData.HasStarData(clusterName)) continue;

            Vector3 position = loadData.clusterPositions[clusterName];

            // Instantiate cluster GameObject
            //GameObject cluster = Instantiate(clusterPrefab, position * scale, Quaternion.identity);

            //we actually want only the negative cluster position (only negative in the x direction)
            Vector3 negXPos = new Vector3(-position.x, position.y, position.z);
            GameObject cluster = Instantiate(clusterPrefab, negXPos * scale, Quaternion.identity);

            cluster.name = clusterName;
            //negCluster.name = "Negative " + clusterName;


            // Optionally parent clusters to a container for organizational purposes
            cluster.transform.SetParent(transform); // Parent to ClusterInstantiator GameObject
        }
    }

    void InstantSun(float[] sunPos)
    {
        Vector3 sunPosVector = new Vector3(sunPos[0], sunPos[1], sunPos[2]);
        GameObject sun = Instantiate(sunPrefab,  sunPosVector * scale, Quaternion.identity);
        sun.name = "Sun";
    }

    void InstantGalaxyCenter()
    {
        GameObject sagittariusA = Instantiate(galaxyCenterPrefab, Vector3.zero * scale, Quaternion.identity);
        sagittariusA.name = "Sagittarius A";
    }
    
}
