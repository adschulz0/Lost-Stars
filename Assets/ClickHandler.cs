using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ClickHandler : MonoBehaviour
{
    private Elements scr_elements;
    private GameObject infoPanel;
    private TextMeshProUGUI clusterName;
    private Text clusterNameText;

    private Color originalColor;
    public Color highlightColor = Color.white;
    private Renderer rend;
    public bool starsVisible = false;
    private bool starsInstantiated = false;

    private StarPlotter starPlotter;
    private LoadData loadData;
    private PlotView2D plotView2D;
    private Movement cameraMovement;
    private GameObject canvasWorldSpace;

    void Start()
    {
        scr_elements = GameObject.Find("Manager").GetComponent<Elements>();
        infoPanel    = scr_elements.clusterInfoPanel;
        clusterName  = scr_elements.clusterName;

        starPlotter    = FindObjectOfType<StarPlotter>();
        loadData       = starPlotter.loadData;
        plotView2D     = FindObjectOfType<PlotView2D>();
        cameraMovement = Camera.main?.GetComponent<Movement>();

        canvasWorldSpace = GameObject.Find("Canvas World Space");
        clusterNameText  = canvasWorldSpace.GetComponentInChildren<Text>();

        if (scr_elements.infoMore != null)
            scr_elements.infoMore.text = "...";

        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;
    }

    void OnMouseEnter()
    {
        if (!Cursor.visible)
            return;

        string name = gameObject.name;
        clusterNameText.text = name;
        clusterNameText.transform.position   = gameObject.transform.position + new Vector3(0, 50, 0);
        clusterNameText.transform.localScale = new Vector3(-1, 1, 1);
        clusterNameText.transform.LookAt(Camera.main.transform);
        clusterNameText.gameObject.SetActive(true);

        if (rend != null && name != "Sun" && name != "Sagittarius A")
            rend.material.color = highlightColor;
    }

    void OnMouseExit()
    {
        clusterNameText.gameObject.SetActive(false);
        clusterNameText.transform.localScale = Vector3.one;

        if (rend != null)
            rend.material.color = originalColor;
    }

    private void OnMouseDown()
    {
        if (plotView2D == null || !plotView2D.InPlotMode)
        {
            string name = gameObject.name;
            clusterName.text = name;

            if (loadData.clusterRsun.TryGetValue(name, out float rsun))
                scr_elements.infoDistFromSun.text = $"R_Sun: {rsun:F2} kpc";
            if (loadData.clusterRGC.TryGetValue(name, out float rgc))
                scr_elements.infoGalactocentricDist.text = $"R_GC: {rgc:F2} kpc";
            if (loadData.clusterRV.TryGetValue(name, out float rv))
                scr_elements.infoRadialVelocity.text = $"RV: {rv:F2} km/s";
            if (loadData.clusterMass.TryGetValue(name, out float mass))
                scr_elements.infoMass.text = $"Mass: {mass:G3} MSun";
            if (loadData.clusterAge.TryGetValue(name, out float age))
                scr_elements.infoAge.text = $"Age: {age / 1e9f:F2} Gyr";

            infoPanel.SetActive(true);
            plotView2D?.SetTargetCluster(name);
            cameraMovement?.LerpToCluster(gameObject.transform.position);
        }

        if (!starsInstantiated)
        {
            starPlotter.PlotStarsForCluster(gameObject.name);
            starsInstantiated = true;
            starsVisible = true;
        }
        else
        {
            starsVisible = !starsVisible;
            foreach (Transform child in transform)
                child.gameObject.SetActive(starsVisible);
        }
    }
}
