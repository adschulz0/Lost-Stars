using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClusterSearch : MonoBehaviour
{
    public InputField searchInputField;
    public GameObject manager;
    public PlotView2D plotView2D;

    private Movement cameraMovement;

    private void Start()
    {
        searchInputField.onValueChanged.AddListener(OnSearch);
        searchInputField.onEndEdit.AddListener(OnEndEdit);
        cameraMovement = Camera.main?.GetComponent<Movement>();
    }

    // onEndEdit fires synchronously on the same frame as the key press, so
    // GetKeyDown is still valid here even though isFocused has already cleared.
    private void OnEndEdit(string _)
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;
        if (plotView2D != null && plotView2D.InPlotMode) return;

        Transform sole    = null;
        int       visible = 0;
        foreach (Transform cluster in manager.transform)
        {
            if (!cluster.gameObject.activeSelf) continue;
            if (cluster.GetComponent<ClickHandler>() == null) continue;
            sole = cluster;
            if (++visible > 1) break;
        }

        if (visible == 1)
            cameraMovement?.LerpToCluster(sole.position);
    }

    private void OnSearch(string searchQuery)
    {
        searchQuery = searchQuery.ToLower();

        foreach (Transform cluster in manager.transform)
        {
            bool clusterMatches = cluster.name.ToLower().Contains(searchQuery);
            cluster.gameObject.SetActive(clusterMatches);
        }

        if (plotView2D != null)
            plotView2D.OnClusterSearchChanged(searchQuery);
    }
}
