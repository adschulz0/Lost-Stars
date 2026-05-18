using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class PlotView2D : MonoBehaviour
{
    [Header("Dependencies")]
    public LoadData    loadData;
    public StarPlotter starPlotter;
    public Movement    cameraMovement;
    public Camera      mainCamera;

    [Header("Info Panel (shared with cluster data)")]
    public GameObject infoPanel;

    [Header("Plot Controls (children of info panel)")]
    public TMP_Dropdown axisXDropdown;
    public TMP_Dropdown axisYDropdown;
    public Button       enterPlotButton;
    public Button       backButton;
    public Button       flipXButton;
    public Button       flipYButton;
    public Button       downloadPlotButton;

    [Header("Other UI")]
    public Button     hideAllStarsButton;
    public InputField searchInputField;

    [Header("Plot Settings")]
    public float plotHalfExtent = 500f;
    public float lerpDuration   = 1.5f;
    public int   tickCount      = 4;
    public float axisLineWidth  = 3f;
    public float tickLength     = 15f;
    public float labelFontSize  = 300f;
    public float titleFontSize  = 300f;

    private static readonly string[] AxisColumns =
    {
        "X", "Y", "Z", "RA", "Dec", "Helio_Dist", "RV", "pm_ra", "pm_dec", "l", "b", "Release_Time"
    };

    private static readonly Dictionary<string, string> ColumnUnits = new Dictionary<string, string>
    {
        { "X",            "kpc"    },
        { "Y",            "kpc"    },
        { "Z",            "kpc"    },
        { "RA",           "°"      },
        { "Dec",          "°"      },
        { "Helio_Dist",   "kpc"    },
        { "RV",           "km/s"   },
        { "pm_ra",        "mas/yr" },
        { "pm_dec",       "mas/yr" },
        { "l",            "°"      },
        { "b",            "°"      },
        { "Release_Time", "Myr"    },
    };

    private static string WithUnit(string col)
    {
        return ColumnUnits.TryGetValue(col, out string unit) ? $"{col} ({unit})" : col;
    }

    private string    targetCluster;
    private bool      inPlotMode;
    private Coroutine activeCoroutine;
    private string    activePlotXCol;
    private string    activePlotYCol;

    private bool flipX;
    private bool flipY;

    private readonly List<GameObject>         axisObjects       = new List<GameObject>();
    private readonly Dictionary<int, Vector3> originalPositions = new Dictionary<int, Vector3>();

    private Vector3    savedCamPos;
    private Quaternion savedCamRot;

    private GameObject sunObject;
    private GameObject sagAObject;

    public bool InPlotMode => inPlotMode;

    // -------------------------------------------------------------------------

    void Start()
    {
        infoPanel.SetActive(false);
        backButton.gameObject.SetActive(false);

        axisXDropdown.ClearOptions();
        axisYDropdown.ClearOptions();
        axisXDropdown.AddOptions(new List<string>(AxisColumns));
        axisYDropdown.AddOptions(new List<string>(AxisColumns));
        axisXDropdown.value = 0;
        axisYDropdown.value = 1;

        enterPlotButton.onClick.AddListener(OnEnterPlot);
        backButton.onClick.AddListener(OnBack);
        if (flipXButton        != null) { flipXButton.onClick.AddListener(OnFlipX);        flipXButton.gameObject.SetActive(false); }
        if (flipYButton        != null) { flipYButton.onClick.AddListener(OnFlipY);        flipYButton.gameObject.SetActive(false); }
        if (downloadPlotButton != null) { downloadPlotButton.onClick.AddListener(OnDownloadPlot); downloadPlotButton.gameObject.SetActive(false); }
    }

    void Update()
    {
        if (inPlotMode) return;
        if (!Input.GetMouseButtonDown(0)) return;

        // Let UI clicks (buttons, dropdowns) pass through without closing the panel.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // If the click didn't land on a cluster collider, close the panel.
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit) ||
            hit.collider == null ||
            hit.collider.GetComponent<ClickHandler>() == null)
        {
            infoPanel.SetActive(false);
            targetCluster = null;
        }
    }

    // Called by ClickHandler when a cluster is clicked.
    public void SetTargetCluster(string clusterName)
    {
        if (inPlotMode) return;
        targetCluster = clusterName;
    }

    // Called by ClusterSearch after each filter pass.
    public void OnClusterSearchChanged(string query)
    {
        if (inPlotMode) return;
        if (string.IsNullOrEmpty(targetCluster)) return;
        bool clusterVisible = targetCluster.ToLower().Contains(query.ToLower());
        infoPanel.SetActive(clusterVisible);
    }

    // -------------------------------------------------------------------------

    private void OnEnterPlot()
    {
        if (string.IsNullOrEmpty(targetCluster)) return;
        if (!loadData.xByCluster.ContainsKey(targetCluster)) return;

        string xCol = AxisColumns[axisXDropdown.value];
        string yCol = AxisColumns[axisYDropdown.value];

        if (inPlotMode && xCol == activePlotXCol && yCol == activePlotYCol) return;

        // Reset flips for every new/changed plot.
        flipX = false;
        flipY = false;
        UpdateFlipVisual(flipXButton, false);
        UpdateFlipVisual(flipYButton, false);

        List<float> dataX = loadData.GetColumnData(targetCluster, xCol);
        List<float> dataY = loadData.GetColumnData(targetCluster, yCol);
        if (dataX == null || dataY == null || dataX.Count == 0) return;

        if (!inPlotMode)
        {
            // Restore all cluster dot visibility and clear the search filter.
            foreach (Transform child in starPlotter.transform)
                child.gameObject.SetActive(true);
            if (searchInputField != null) searchInputField.text = "";
        }

        activePlotXCol = xCol;
        activePlotYCol = yCol;

        if (activeCoroutine != null) StopCoroutine(activeCoroutine);

        if (inPlotMode)
            activeCoroutine = StartCoroutine(RePlot(dataX, dataY, xCol, yCol));
        else
            activeCoroutine = StartCoroutine(LerpTo2D(targetCluster, dataX, dataY, xCol, yCol));
    }

    private void OnBack()
    {
        if (!inPlotMode) return;
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(LerpTo3D());
    }

    private void OnFlipX()
    {
        flipX = !flipX;
        UpdateFlipVisual(flipXButton, flipX);
        if (inPlotMode) RefreshFlip();
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnFlipY()
    {
        flipY = !flipY;
        UpdateFlipVisual(flipYButton, flipY);
        if (inPlotMode) RefreshFlip();
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void RefreshFlip()
    {
        List<float> dataX = loadData.GetColumnData(targetCluster, activePlotXCol);
        List<float> dataY = loadData.GetColumnData(targetCluster, activePlotYCol);
        if (dataX == null || dataY == null) return;
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(RePlot(dataX, dataY, activePlotXCol, activePlotYCol));
    }

    private void OnDownloadPlot()
    {
        if (!inPlotMode || string.IsNullOrEmpty(targetCluster)) return;

        List<float> dataX  = loadData.GetColumnData(targetCluster, activePlotXCol);
        List<float> dataY  = loadData.GetColumnData(targetCluster, activePlotYCol);
        List<float> dataRT = loadData.GetColumnData(targetCluster, "Release_Time");
        if (dataX == null || dataY == null || dataRT == null || dataX.Count == 0) return;

        int n = Mathf.Min(dataX.Count, Mathf.Min(dataY.Count, dataRT.Count));

        string safeName = string.Concat(targetCluster.Split(Path.GetInvalidFileNameChars()));
        string csvPath  = Path.Combine(Path.GetTempPath(),
            $"lost_stars_{safeName}_{System.DateTime.Now:yyyyMMddHHmmss}.csv");

        Debug.Log($"[DownloadPlot] Temp CSV: {csvPath}");

        using (var writer = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8))
        {
            writer.WriteLine("x,y,release_time");
            for (int i = 0; i < n; i++)
                writer.WriteLine($"{dataX[i]},{dataY[i]},{dataRT[i]}");
        }

        string[] csvPreview = File.ReadAllLines(csvPath);
        Debug.Log($"[DownloadPlot] CSV preview:\n{string.Join("\n", csvPreview, 0, Mathf.Min(3, csvPreview.Length))}");

        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string pngPath     = Path.Combine(desktopPath, $"{safeName}_{activePlotXCol}_vs_{activePlotYCol}.png");
        string scriptPath  = Path.Combine(Application.streamingAssetsPath, "plot.py");

        Debug.Log($"[DownloadPlot] Output PNG: {pngPath}");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "python",
            Arguments              = $"\"{scriptPath}\" \"{csvPath}\" \"{targetCluster}\" \"{activePlotXCol}\" \"{activePlotYCol}\" \"{pngPath}\"",
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        };

        using (var proc = System.Diagnostics.Process.Start(psi))
        {
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (!string.IsNullOrWhiteSpace(stdout)) Debug.Log($"[DownloadPlot] {stdout.Trim()}");
            if (!string.IsNullOrWhiteSpace(stderr)) Debug.LogError($"[DownloadPlot] {stderr.Trim()}");
        }
    }

    private static void UpdateFlipVisual(Button btn, bool active) { }

    private void SetPlotControlsInteractable(bool interactable)
    {
        enterPlotButton.interactable          = interactable;
        axisXDropdown.interactable            = interactable;
        axisYDropdown.interactable            = interactable;
        if (flipXButton        != null) flipXButton.interactable        = interactable;
        if (flipYButton        != null) flipYButton.interactable        = interactable;
        if (downloadPlotButton != null) downloadPlotButton.interactable = interactable;
    }

    // -------------------------------------------------------------------------

    private IEnumerator LerpTo2D(string cluster, List<float> dataX, List<float> dataY,
                                  string xCol, string yCol)
    {
        inPlotMode = true;
        backButton.gameObject.SetActive(true);
        if (flipXButton        != null) flipXButton.gameObject.SetActive(true);
        if (flipYButton        != null) flipYButton.gameObject.SetActive(true);
        if (downloadPlotButton != null) downloadPlotButton.gameObject.SetActive(true);
        SetPlotControlsInteractable(false);
        if (hideAllStarsButton != null) hideAllStarsButton.interactable = false;
        if (searchInputField   != null) searchInputField.interactable   = false;

        Cursor.lockState       = CursorLockMode.None;
        Cursor.visible         = true;
        cameraMovement.enabled = false;

        HideSceneObjects();

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
            originalPositions[id] = star.position;
            from[i] = star.position;

            float nx = (maxX > minX) ? (dataX[i] - minX) / (maxX - minX) : 0.5f;
            float ny = (maxY > minY) ? (dataY[i] - minY) / (maxY - minY) : 0.5f;
            to[i] = StarPlotPosition(nx, ny);
        }

        // Camera at +Z looking along -Z, consistent with the default starting orientation.
        float      camDist   = plotHalfExtent / Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.4f;
        Vector3    tgtCamPos = new Vector3(0f, 0f, camDist);
        Quaternion tgtCamRot = Quaternion.Euler(0f, 180f, 0f);

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
        SetPlotControlsInteractable(true);
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

    private IEnumerator RePlot(List<float> dataX, List<float> dataY, string xCol, string yCol)
    {
        SetPlotControlsInteractable(false);
        ClearAxes();

        Transform clusterTf = starPlotter.transform.Find(targetCluster);
        if (clusterTf == null) { SetPlotControlsInteractable(true); yield break; }

        int n = Mathf.Min(clusterTf.childCount, Mathf.Min(dataX.Count, dataY.Count));

        float minX, maxX, minY, maxY;
        ComputeRange(dataX, n, out minX, out maxX);
        ComputeRange(dataY, n, out minY, out maxY);

        var from = new Vector3[n];
        var to   = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            from[i] = clusterTf.GetChild(i).position;

            float nx = (maxX > minX) ? (dataX[i] - minX) / (maxX - minX) : 0.5f;
            float ny = (maxY > minY) ? (dataY[i] - minY) / (maxY - minY) : 0.5f;
            to[i] = StarPlotPosition(nx, ny);
        }

        float elapsed = 0f;
        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / lerpDuration);
            for (int i = 0; i < n; i++)
                clusterTf.GetChild(i).position = Vector3.Lerp(from[i], to[i], t);
            yield return null;
        }

        for (int i = 0; i < n; i++)
            clusterTf.GetChild(i).position = to[i];

        DrawAxes(minX, maxX, minY, maxY, xCol, yCol);
        SetPlotControlsInteractable(true);
    }

    // Maps normalised [0,1] coordinates to world plot position, respecting flip state.
    // Unflipped X: min maps to screen-left (world +h), max to screen-right (world -h).
    // Unflipped Y: min maps to screen-bottom (world -h), max to screen-top (world +h).
    private Vector3 StarPlotPosition(float nx, float ny)
    {
        float px = (flipX ? (nx - 0.5f) : (0.5f - nx)) * plotHalfExtent * 2f;
        float py = (flipY ? (0.5f - ny) : (ny - 0.5f)) * plotHalfExtent * 2f;
        return new Vector3(px, py, 0f);
    }

    private void CleanupPlotMode()
    {
        inPlotMode             = false;
        cameraMovement.enabled = true;
        backButton.gameObject.SetActive(false);
        if (flipXButton        != null) flipXButton.gameObject.SetActive(false);
        if (flipYButton        != null) flipYButton.gameObject.SetActive(false);
        if (downloadPlotButton != null) downloadPlotButton.gameObject.SetActive(false);
        if (hideAllStarsButton != null) hideAllStarsButton.interactable = true;
        if (searchInputField   != null) searchInputField.interactable   = true;
        activePlotXCol = null;
        activePlotYCol = null;
        ShowSceneObjects();
    }

    // -------------------------------------------------------------------------
    // Scene object visibility
    // -------------------------------------------------------------------------

    private void HideSceneObjects()
    {
        if (sunObject == null) sunObject = GameObject.Find("Sun");
        if (sagAObject == null) sagAObject = GameObject.Find("Sagittarius A");

        foreach (Transform child in starPlotter.transform)
        {
            if (child.name == targetCluster)
            {
                // Only disable the renderer — SetActive(false) would also hide the star children.
                Renderer r = child.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }

        if (sunObject != null) sunObject.SetActive(false);
        if (sagAObject != null) sagAObject.SetActive(false);
    }

    private void ShowSceneObjects()
    {
        foreach (Transform child in starPlotter.transform)
        {
            if (child.name == targetCluster)
            {
                Renderer r = child.GetComponent<Renderer>();
                if (r != null) r.enabled = true;
            }
            else
            {
                child.gameObject.SetActive(true);
            }
        }

        if (sunObject != null) sunObject.SetActive(true);
        if (sagAObject != null) sagAObject.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Axis drawing
    // -------------------------------------------------------------------------

    private void DrawAxes(float minX, float maxX, float minY, float maxY,
                           string xLabel, string yLabel)
    {
        float h   = plotHalfExtent;
        float tl  = tickLength;
        float gap = tl + 10f;

        CreateLine(new Vector3(-h, -h, 0f), new Vector3( h, -h, 0f), axisLineWidth, "XAxis");
        CreateLine(new Vector3( h, -h, 0f), new Vector3( h,  h, 0f), axisLineWidth, "YAxis");

        for (int i = 0; i <= tickCount; i++)
        {
            float frac = i / (float)tickCount;
            float xw   = Mathf.Lerp(-h, h, frac);
            float yw   = Mathf.Lerp(-h, h, frac);

            // Tick values respect flip: unflipped X runs max→min as xw goes -h→+h
            // (because screen-left = world +h = data min for unflipped).
            float xv = flipX ? Mathf.Lerp(minX, maxX, frac) : Mathf.Lerp(maxX, minX, frac);
            float yv = flipY ? Mathf.Lerp(maxY, minY, frac) : Mathf.Lerp(minY, maxY, frac);

            CreateLine(new Vector3(xw, -h, 0f), new Vector3(xw, -h - tl, 0f),
                       axisLineWidth * 0.5f, $"XTick{i}");
            CreateLabel(xv.ToString("F1"),
                        new Vector3(xw, -h - tl - gap, 0f),
                        labelFontSize, TextAlignmentOptions.Center);

            CreateLine(new Vector3(h, yw, 0f), new Vector3(h + tl, yw, 0f),
                       axisLineWidth * 0.5f, $"YTick{i}");
            CreateLabel(yv.ToString("F1"),
                        new Vector3(h + tl + gap * 2f, yw, 0f),
                        labelFontSize, TextAlignmentOptions.Right);
        }

        CreateLabel(WithUnit(xLabel),
                    new Vector3(0f, -h - tl - gap * 3f, 0f),
                    titleFontSize, TextAlignmentOptions.Center);

        var yTitleGo = new GameObject("PlotLabel_YTitle");
        axisObjects.Add(yTitleGo);
        var yTmp = yTitleGo.AddComponent<TextMeshPro>();
        yTmp.text                  = WithUnit(yLabel);
        yTmp.fontSize              = titleFontSize;
        yTmp.alignment             = TextAlignmentOptions.Center;
        yTmp.color                 = Color.black;
        yTmp.enableWordWrapping    = false;
        yTitleGo.transform.position = new Vector3(h + tl + gap * 7f, 0f, 0f);
        yTitleGo.transform.rotation = Quaternion.Euler(0f, 180f, 90f);
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
        lr.startColor    = lr.endColor = Color.black;
        lr.useWorldSpace = true;
    }

    private void CreateLabel(string text, Vector3 position, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject("PlotLabel");
        axisObjects.Add(go);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text               = text;
        tmp.fontSize           = fontSize;
        tmp.alignment          = alignment;
        tmp.color              = Color.black;
        tmp.enableWordWrapping = false;
        go.transform.position  = position;
        go.transform.rotation  = Quaternion.Euler(0f, 180f, 0f);
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
