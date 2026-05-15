using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarPlotter : MonoBehaviour
{
    public float scale = 100.0f; // Scale factor for positioning

    public LoadData loadData; // Reference to the LoadData script

    public GameObject starPrefab; // Reference to the star prefab
    public GameObject clusterPrefab;
    public GameObject sunPrefab;
    public GameObject galaxyCenterPrefab;

    public float[] sunPos = new float[3];

    public int numToPlot;

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

        //PlotStars();

        InstantClusters();
        InstantGalaxyCenter();
        InstantSun(sunPos);

        PlotStars();
    }

    void PlotStars()
    {
        List<string> clusterNames = new List<string>(loadData.xByCluster.Keys);  // Get list of cluster names
        //List<string> clusterNames = new List<string>();
        //clusterNames.Add("NGC_6569");

        //Debug.Log(clusterNames.Count);
        //clusterNames.Count
        for (int i = 0; i < numToPlot; i++)
        {
            string clusterName = clusterNames[i];

            // Get the cluster position
            //Vector3 clusterPosition = loadData.clusterPositions[clusterName];

            List<float> xArray = loadData.xByCluster[clusterName];
            List<float> yArray = loadData.yByCluster[clusterName];
            List<float> zArray = loadData.zByCluster[clusterName];

            if (xArray == null || yArray == null || zArray == null)
            {
                Debug.LogError($"Star arrays for cluster {clusterName} are null!");
                continue;
            }

            if (xArray.Count != yArray.Count || xArray.Count != zArray.Count)
            {
                Debug.LogError($"Star arrays for cluster {clusterName} have different lengths!");
                continue;
            }

            //Debug.Log($"Number of stars to plot for cluster {clusterName}: {xArray.Count}");

            // Find the cluster GameObject by name
            GameObject cluster = GameObject.Find(clusterName);

            if (cluster == null)
            {
                Debug.LogError($"Cluster GameObject {clusterName} not found!");
                continue;
            }

            // Iterate over star positions in this cluster
            for (int j = 0; j < xArray.Count; j++)
            {
                Vector3 position = new Vector3(xArray[j], yArray[j], zArray[j]);

                // Instantiate star GameObject
                GameObject star = Instantiate(starPrefab, position * scale, Quaternion.identity);

                //change the scale
                star.transform.localScale *= 1;

                // Optionally parent stars to a container for organizational purposes

                star.transform.SetParent(cluster.transform); // Parent to StarPlotter GameObject

                star.SetActive(false);

                // Customize further properties of the star GameObject here
                // For example, adjust material, color, etc.
            }
        }
    }

    void InstantClusters()
    {
        // Ensure references are correctly assigned
        if (loadData == null)
        {
            Debug.LogError("LoadData script reference not set!");
            return;
        }

        List<string> clusterNames = new List<string>(loadData.xByCluster.Keys);  // Get list of cluster names

        numToPlot = Mathf.Min(numToPlot, clusterNames.Count);

        //clusterNames.Count
        for (int i = 0; i < numToPlot; i++)
        {
            string clusterName = clusterNames[i];

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
