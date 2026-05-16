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

    private Coroutine revealCoroutine;

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
        }

        if (!starsInstantiated)
        {
            starPlotter.PlotStarsForCluster(gameObject.name);
            starsInstantiated = true;
            starsVisible = true;
            if (plotView2D == null || !plotView2D.InPlotMode)
                cameraMovement?.LerpToCluster(gameObject.transform.position);
            // Stars are spawned active by StarPlotter; hide them all first, then reveal gradually.
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
            StartReveal();
        }
        else
        {
            starsVisible = !starsVisible;
            if (starsVisible)
            {
                if (plotView2D == null || !plotView2D.InPlotMode)
                    cameraMovement?.LerpToCluster(gameObject.transform.position);
                StartReveal();
            }
            else
            {
                if (revealCoroutine != null)
                {
                    StopCoroutine(revealCoroutine);
                    revealCoroutine = null;
                }
                foreach (Transform child in transform)
                    child.gameObject.SetActive(false);

                if ((plotView2D == null || !plotView2D.InPlotMode) &&
                    clusterName.text == gameObject.name)
                {
                    infoPanel.SetActive(false);
                }
            }
        }
    }

    private void StartReveal()
    {
        if (revealCoroutine != null) StopCoroutine(revealCoroutine);
        revealCoroutine = StartCoroutine(RevealStars());
    }

    private IEnumerator RevealStars()
    {
        var stars = new List<Transform>();
        foreach (Transform child in transform)
            stars.Add(child);

        if (stars.Count == 0) { revealCoroutine = null; yield break; }

        // Fisher-Yates shuffle
        for (int i = stars.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (stars[i], stars[j]) = (stars[j], stars[i]);
        }

        // Star i is scheduled to appear at elapsed time i * (2 / n).
        // Star 0 appears immediately; all stars appear within 2 seconds.
        int   n       = stars.Count;
        float elapsed = 0f;
        int   next    = 0;

        while (next < n)
        {
            if (!starsVisible) yield break;

            while (next < n && elapsed >= (float)next * 2f / n)
            {
                stars[next].gameObject.SetActive(true);
                next++;
            }

            if (next < n)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        revealCoroutine = null;
    }
}
