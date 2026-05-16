using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StarColorMapper : MonoBehaviour
{
    [Header("Dependencies")]
    public LoadData     loadData;
    public StarPlotter  starPlotter;
    public TMP_Dropdown dropdown;

    [Header("Colorbar UI")]
    public RawImage         colorbarImage;
    public TextMeshProUGUI  colorbarTitle;
    public TextMeshProUGUI  colorbarMin;
    public TextMeshProUGUI  colorbarMax;

    [Header("Gradient")]
    public Color color0 = new Color( 68f / 255f,   1f / 255f,  84f / 255f); // #440154
    public Color color1 = new Color( 49f / 255f, 104f / 255f, 142f / 255f); // #31688e
    public Color color2 = new Color( 53f / 255f, 183f / 255f, 121f / 255f); // #35b779
    public Color color3 = new Color(253f / 255f, 231f / 255f,  37f / 255f); // #fde725

    // Column names exactly as used in GetColumnData / CSV header.
    // Order determines dropdown order; Release_Time is last and is the default.
    private static readonly string[] Columns =
    {
        "RA", "Dec", "Helio_Dist", "RV", "pm_ra", "pm_dec", "l", "b", "Release_Time"
    };

    private static readonly Dictionary<string, string> ColumnUnits = new Dictionary<string, string>
    {
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

    private string selectedColumn = "Release_Time";
    private bool   noneSelected   = false;

    // -------------------------------------------------------------------------

    void Start()
    {
        dropdown.ClearOptions();
        var options = new List<string>(Columns) { "None" };
        dropdown.AddOptions(options);
        dropdown.value = Array.IndexOf(Columns, "Release_Time");
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(OnDropdownChanged);

        BuildColorbarTexture();
        colorbarTitle.text = WithUnit(selectedColumn);
        colorbarMin.text   = "—";
        colorbarMax.text   = "—";
    }

    // -------------------------------------------------------------------------
    // Dropdown callback
    // -------------------------------------------------------------------------

    private void OnDropdownChanged(int index)
    {
        if (index >= Columns.Length)
        {
            noneSelected       = true;
            colorbarTitle.text = "None";
            PaintAllBlack();
            return;
        }
        noneSelected       = false;
        selectedColumn     = Columns[index];
        colorbarTitle.text = WithUnit(selectedColumn);
        RecolorAllVisible();
    }

    // -------------------------------------------------------------------------
    // Public: called by StarPlotter after a cluster's stars are instantiated
    // -------------------------------------------------------------------------

    public void ColorizeCluster(string clusterName)
    {
        Transform clusterTf = starPlotter.transform.Find(clusterName);
        if (clusterTf == null || clusterTf.childCount == 0) return;

        if (noneSelected)
        {
            ApplyBlack(clusterTf);
            return;
        }

        List<float> data = loadData.GetColumnData(clusterName, selectedColumn);
        if (data == null || data.Count == 0) return;

        float min, max;
        ComputeRange(data, out min, out max);
        ApplyColors(clusterTf, data, min, max);

        // Refresh colorbar range across all visible clusters (includes this one)
        UpdateColorbarRange();
    }

    // -------------------------------------------------------------------------
    // Recolor every cluster that currently has visible stars
    // -------------------------------------------------------------------------

    private void RecolorAllVisible()
    {
        foreach (Transform clusterTf in starPlotter.transform)
        {
            if (clusterTf.childCount == 0) continue;
            if (!clusterTf.GetChild(0).gameObject.activeSelf) continue;

            List<float> data = loadData.GetColumnData(clusterTf.name, selectedColumn);
            if (data == null || data.Count == 0) continue;

            float min, max;
            ComputeRange(data, out min, out max);
            ApplyColors(clusterTf, data, min, max);
        }

        UpdateColorbarRange();
    }

    // -------------------------------------------------------------------------
    // Core helpers
    // -------------------------------------------------------------------------

    private void ApplyBlack(Transform clusterTf)
    {
        var mpb = new MaterialPropertyBlock();
        foreach (Transform star in clusterTf)
        {
            Renderer r = star.GetComponent<Renderer>();
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", Color.black);
            r.SetPropertyBlock(mpb);
        }
    }

    private void PaintAllBlack()
    {
        foreach (Transform clusterTf in starPlotter.transform)
            ApplyBlack(clusterTf);
        colorbarMin.text = "—";
        colorbarMax.text = "—";
    }

    private void ApplyColors(Transform clusterTf, List<float> data, float min, float max)
    {
        float range = max - min;
        var mpb = new MaterialPropertyBlock();
        int starIdx = 0;

        foreach (Transform star in clusterTf)
        {
            if (starIdx >= data.Count) break;

            float t = range > 0f ? (data[starIdx] - min) / range : 0.5f;
            Color c = GradientColor(t);

            Renderer r = star.GetComponent<Renderer>();
            if (r != null)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_Color", c);
                r.SetPropertyBlock(mpb);
            }

            starIdx++;
        }
    }

    private static void ComputeRange(List<float> data, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;
        foreach (float v in data)
        {
            if (v < min) min = v;
            if (v > max) max = v;
        }
    }

    // Compute global min/max across all clusters that have visible stars and update labels.
    private void UpdateColorbarRange()
    {
        if (noneSelected) { colorbarMin.text = "—"; colorbarMax.text = "—"; return; }

        float globalMin = float.MaxValue;
        float globalMax = float.MinValue;
        bool  any       = false;

        foreach (Transform clusterTf in starPlotter.transform)
        {
            if (clusterTf.childCount == 0) continue;
            if (!clusterTf.GetChild(0).gameObject.activeSelf) continue;

            List<float> data = loadData.GetColumnData(clusterTf.name, selectedColumn);
            if (data == null || data.Count == 0) continue;

            float min, max;
            ComputeRange(data, out min, out max);
            if (min < globalMin) globalMin = min;
            if (max > globalMax) globalMax = max;
            any = true;
        }

        string unitSuffix = ColumnUnits.TryGetValue(selectedColumn, out string u) ? " " + u : "";
        colorbarMin.text = any ? globalMin.ToString("F2") + unitSuffix : "—";
        colorbarMax.text = any ? globalMax.ToString("F2") + unitSuffix : "—";
    }

    // -------------------------------------------------------------------------
    // Viridis colormap — four keypoints linearly interpolated
    // -------------------------------------------------------------------------

    private Color GradientColor(float t)
    {
        t = Mathf.Clamp01(t);

        // Collect Inspector colors that are active (alpha > 0), in order
        Color[] all    = { color0, color1, color2, color3 };
        var     active = new List<Color>(4);
        foreach (Color c in all)
            if (c.a > 0f) active.Add(c);

        if (active.Count == 0) return Color.white;
        if (active.Count == 1) return active[0];

        // Distribute active colors evenly across [0, 1] and interpolate
        float scaledT = t * (active.Count - 1);
        int   lo      = Mathf.FloorToInt(scaledT);
        int   hi      = Mathf.Min(lo + 1, active.Count - 1);
        return Color.Lerp(active[lo], active[hi], scaledT - lo);
    }

    // -------------------------------------------------------------------------
    // Colorbar texture (viridis gradient, 1×256)
    // -------------------------------------------------------------------------

    private void BuildColorbarTexture()
    {
        var tex = new Texture2D(1, 256, TextureFormat.RGB24, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };

        var pixels = new Color[256];
        for (int i = 0; i < 256; i++)
            pixels[i] = GradientColor(i / 255f);

        tex.SetPixels(pixels);
        tex.Apply();
        colorbarImage.texture = tex;
    }
}
