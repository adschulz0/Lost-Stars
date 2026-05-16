using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buttons : MonoBehaviour
{
    public GameObject infoPanel;

    private GameObject managerObject;

    void Start()
    {
        managerObject = GameObject.Find("Manager");
    }

    public void HideAllStars()
    {
        if (infoPanel != null) infoPanel.SetActive(false);

        if (managerObject != null)
        {
            // Iterate through all clusters under the manager GameObject
            Transform[] clusters = managerObject.GetComponentsInChildren<Transform>();
            foreach (Transform cluster in clusters)
            {
                // Check if the current object is a cluster (skip the manager itself)
                if (cluster != managerObject.transform && cluster.childCount > 0)
                {
                    // Get the first star (assuming all stars in a cluster are managed similarly)
                    GameObject firstStar = cluster.GetChild(0).gameObject;

                    // Check if the first star is active
                    if (firstStar.activeSelf)
                    {
                        cluster.GetComponent<ClickHandler>().starsVisible = false;
                        // Toggle all stars in the cluster based on the first star
                        foreach (Transform star in cluster)
                        {
                            star.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }
}
