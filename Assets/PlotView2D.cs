using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlotView2D : MonoBehaviour
{
    [Header("Dependencies")]
    public LoadData    loadData;
    public StarPlotter starPlotter;
    public Movement    cameraMovement;
    public Camera      mainCamera;

    [Header("Plot Panel UI")]
    public GameObject  plotPanel;
    public TMP_Dropdown axisXDropdown;
    public TMP_Dropdown axisYDropdown;
    public Button      enterPlotButton;
    public Button      backButton;

    [Header("Plot Settings")]
    public float plotHalfExtent = 500f;
    public float lerpDuration   = 1.5f;
    public int   tickCount      = 8;
    public float axisLineWidth  = 3f;
    public float tickLength     = 15f;
    public float labelFontSize  = 22f;
    public float titleFontSize  = 30f;

    private static readonly string[] AxisColumns =
    {
        "RA", "Dec", "Helio_Dist", "RV", "pm_ra", "pm_dec", "l", "b", "Release_Time"
    };

    private string  targetCluster;
    private bool    inPlotMode;
    private Coroutine activeCoroutine;

    private readonly List<GameObject>       axisObjects       = new List<GameObject>();
    private readonly Dictionary<int, Vector3> originalPositions = new Dictionary<int, Vector3>();

    private Vector3    savedCamPos;
    private Quaternion savedCamRot;

    void Start()
    {
        plotPanel.SetActive(false);
        backButton.gameObject.SetActive(false);

        axisXDropdown.ClearOptions();
        axisYDropdown.ClearOptions();
        axisXDropdown.AddOptions(new List<string>(AxisColumns));
        axisYDropdown.AddOptions(new List<string>(AxisColumns));
        axisXDropdown.value = 0; // RA
        axisYDropdown.value = 1; // Dec

        enterPlotButton.onClick.AddListener(OnEnterPlot);
        backButton.onClick.AddListener(OnBack);
    }

    // Called by ClickHandler when a cluster's stars become visible.
    public void NotifyClusterClicked(string clusterName)
    {
        if (inPlotMode) return;
        targetCluster = clusterName;
        plotPanel.SetActive(true);
    }

    // -------------------------------------------------------------------------

    private void OnEnterPlot()
    {
        if (string.IsNullOrEmpty(targetCluster)) return;
        if (!loadData.xByCluster.ContainsKey(targetCluster)) return;

        string xCol  = AxisColumns[axisXDropdown.value];
        string yCol  = AxisColumns[axisYDropdown.value];
        List<float> dataX = loadData.GetColumnData(targetCluster, xCol);
        List<float> dataY = loadData.GetColumnData(targetCluster, yCol);
        if (dataX == null || dataY == null || dataX.Count == 0) return;

        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(LerpTo2D(targetCluster, dataX, dataY, xCol, yCol));
    }

    private void OnBack()
    {
        if (!inPlotMode) return;
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(LerpTo3D());
    }

    // -------------------------------------------------------------------------

    private IEnumerator LerpTo2D(string cluster, List<float> dataX, List<float> dataY,
                                  string xCol, string yCol)
    {
        inPlotMode = true;
        plotPanel.SetActive(false);
        backButton.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        cameraMovement.enabled = false;

        Transform clusterTf = starPlotter.transform.Find(cluster);
        if (clusterTf == null) { CleanupPlotMode(); yield break; }

        int n = Mathf.Min(clusterTf.childCount, Mathf.Min(dataX.Count, dataY.Count));

        float minX, maxX, minY, maxY;
        ComputeRange(dataX, n, out minX, out maxX);
        ComputeRange(dataY, n, out minY, out maxY);

        var from = new Vector3[n];
        var to   = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            Transform star = clusterTf.GetChild(i);
            int id = star.gameObject.GetInstanceID();
            if (!originalPositions.ContainsKey(id))
                originalPositions[id] = star.position;
            from[i] = star.position;

            float nx = (maxX > minX) ? (dataX[i] - minX) / (maxX - minX) : 0.5f;
            float ny = (maxY > minY) ? (dataY[i] - minY) / (maxY - minY) : 0.5f;
            to[i] = new Vector3((nx - 0.5f) * plotHalfExtent * 2f,
                                (ny - 0.5f) * plotHalfExtent * 2f,
                                0f);
        }

        // Camera at -Z looking along +Z so TMP text (front faces -Z) is readable.
        float      camDist   = plotHalfExtent / Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.4f;
        Vector3    tgtCamPos = new Vector3(0f, 0f, -camDist);
        Quaternion tgtCamRot = Quaternion.identity;

        savedCamPos = mainCamera.transform.position;
        savedCamRot = mainCamera.transform.rotation;

        float elapsed = 0f;
        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / lerpDuration);
            for (int i = 0; i < n; i++)
                clusterTf.GetChild(i).position = Vector3.Lerp(from[i], to[i], t);
            mainCamera.transform.position = Vector3.Lerp(savedCamPos, tgtCamPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(savedCamRot, tgtCamRot, t);
            yield return null;
        }

        for (int i = 0; i < n; i++)
            clusterTf.GetChild(i).position = to[i];
        mainCamera.transform.SetPositionAndRotation(tgtCamPos, tgtCamRot);

        DrawAxes(minX, maxX, minY, maxY, xCol, yCol);
    }

    private IEnumerator LerpTo3D()
    {
        ClearAxes();

        Transform clusterTf = starPlotter.transform.Find(targetCluster);
        int n = clusterTf != null ? clusterTf.childCount : 0;

        var from = new Vector3[n];
        var to   = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            Transform star = clusterTf.GetChild(i);
            from[i] = star.position;
            int id  = star.gameObject.GetInstanceID();
            to[i]   = originalPositions.TryGetValue(id, out Vector3 orig) ? orig : star.position;
        }

        Vector3    startCamPos = mainCamera.transform.position;
        Quaternion startCamRot = mainCamera.transform.rotation;

        float elapsed = 0f;
        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / lerpDuration);
            for (int i = 0; i < n; i++)
                clusterTf.GetChild(i).position = Vector3.Lerp(from[i], to[i], t);
            mainCamera.transform.position = Vector3.Lerp(startCamPos, savedCamPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startCamRot, savedCamRot, t);
            yield return null;
        }

        for (int i = 0; i < n; i++)
            clusterTf.GetChild(i).position = to[i];
        mainCamera.transform.SetPositionAndRotation(savedCamPos, savedCamRot);

        CleanupPlotMode();
    }

    private void CleanupPlotMode()
    {
        inPlotMode             = false;
        cameraMovement.enabled = true;
        backButton.gameObject.SetActive(false);
        if (!string.IsNullOrEmpty(targetCluster))
            plotPanel.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Axis drawing
    // -------------------------------------------------------------------------

    private void DrawAxes(float minX, float maxX, float minY, float maxY,
                           string xLabel, string yLabel)
    {
        float h  = plotHalfExtent;
        float tl = tickLength;
        float gap = tl + 10f; // space between tick end and label start

        // Main axis lines
        CreateLine(new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), axisLineWidth, "XAxis");
        CreateLine(new Vector3(-h, -h, 0f), new Vector3(-h, h, 0f), axisLineWidth, "YAxis");

        for (int i = 0; i <= tickCount; i++)
        {
            float frac = i / (float)tickCount;
            float xw   = Mathf.Lerp(-h, h, frac);
            float yw   = Mathf.Lerp(-h, h, frac);
            float xv   = Mathf.Lerp(minX, maxX, frac);
            float yv   = Mathf.Lerp(minY, maxY, frac);

            // X-axis tick + label below
            CreateLine(new Vector3(xw, -h, 0f), new Vector3(xw, -h - tl, 0f),
                       axisLineWidth * 0.5f, $"XTick{i}");
            CreateLabel(xv.ToString("G4"),
                        new Vector3(xw, -h - tl - gap, 0f),
                        labelFontSize, TextAlignmentOptions.Center);

            // Y-axis tick + label to the left
            CreateLine(new Vector3(-h, yw, 0f), new Vector3(-h - tl, yw, 0f),
                       axisLineWidth * 0.5f, $"YTick{i}");
            CreateLabel(yv.ToString("G4"),
                        new Vector3(-h - tl - gap * 4f, yw, 0f),
                        labelFontSize, TextAlignmentOptions.Right);
        }

        // Axis titles
        CreateLabel(xLabel,
                    new Vector3(0f, -h - tl - gap * 5f, 0f),
                    titleFontSize, TextAlignmentOptions.Center);

        // Y title rotated 90° so it reads bottom-to-top
        var yTitleGo = new GameObject("PlotLabel_YTitle");
        axisObjects.Add(yTitleGo);
        var yTmp = yTitleGo.AddComponent<TextMeshPro>();
        yTmp.text      = yLabel;
        yTmp.fontSize  = titleFontSize;
        yTmp.alignment = TextAlignmentOptions.Center;
        yTmp.color     = Color.white;
        yTitleGo.transform.position = new Vector3(-h - tl - gap * 11f, 0f, 0f);
        yTitleGo.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
    }

    private void CreateLine(Vector3 start, Vector3 end, float width, string tag)
    {
        var go = new GameObject("PlotAxis_" + tag);
        axisObjects.Add(go);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPositions(new[] { start, end });
        lr.startWidth    = lr.endWidth = width;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.startColor    = lr.endColor = Color.white;
        lr.useWorldSpace = true;
    }

    private void CreateLabel(string text, Vector3 position, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject("PlotLabel");
        axisObjects.Add(go);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = alignment;
        tmp.color     = Color.white;
        go.transform.position = position;
    }

    private void ClearAxes()
    {
        foreach (var go in axisObjects)
            if (go != null) Destroy(go);
        axisObjects.Clear();
    }

    // -------------------------------------------------------------------------

    private static void ComputeRange(List<float> data, int count, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;
        for (int i = 0; i < count && i < data.Count; i++)
        {
            if (data[i] < min) min = data[i];
            if (data[i] > max) max = data[i];
        }
    }
}
