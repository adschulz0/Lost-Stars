using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClusterSearch : MonoBehaviour
{
    public InputField searchInputField; // Reference to the Input Field
    public GameObject manager; // Reference to the Manager GameObject

    private void Start()
    {
        // Assign the OnValueChanged event to call the OnSearch method
        searchInputField.onValueChanged.AddListener(OnSearch);
    }

    private void OnSearch(string searchQuery)
    {
        searchQuery = searchQuery.ToLower();

        // Loop through all clusters
        foreach (Transform cluster in manager.transform)
        {
            bool clusterMatches = cluster.name.ToLower().Contains(searchQuery);

            // Set the cluster active or inactive based on search match
            cluster.gameObject.SetActive(clusterMatches);
        }
    }
}
