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

    private Text clusterNameText; // Reference to the Text UI element in the World Space Canvas

    private Color originalColor;
    public Color highlightColor = Color.white; // Bluish color
    private Renderer rend;
    public bool starsVisible = false;

    private GameObject canvasWorldSpace;

    public bool clicked;

    void Start()
    {
        scr_elements = GameObject.Find("Manager").GetComponent<Elements>();
        infoPanel = scr_elements.clusterInfoPanel;
        clusterName = scr_elements.clusterName;

        canvasWorldSpace = GameObject.Find("Canvas World Space");
        clusterNameText = canvasWorldSpace.GetComponentInChildren<Text>();


        // Store the original color of the GameObject
        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            originalColor = rend.material.color;
        }
    }

    void OnMouseEnter()
    {
        bool isCursorVisible = Cursor.visible;
        CursorLockMode lockMode = Cursor.lockState;

        if (!Cursor.visible)
            return;

        infoPanel.SetActive(true);
        clusterName.text = gameObject.name;

        clusterNameText.text = gameObject.name; // Update the Text UI element with cluster name
        clusterNameText.transform.position = gameObject.transform.position + new Vector3(0, 50 , 0);
        clusterNameText.transform.localScale = new Vector3(-1, 1, 1);
        clusterNameText.transform.LookAt(Camera.main.transform); // Face the camera
        clusterNameText.gameObject.SetActive(true); // Show the Text UI element

        // Change the GameObject's color to highlightColor
        if (rend != null && (gameObject.name != "Sun" && gameObject.name != "Sagittarius A"))
        {
            rend.material.color = highlightColor;
        }
    }

    void OnMouseExit()
    {
        if (clicked)
            return;

        infoPanel.SetActive(false);

        clusterNameText.gameObject.SetActive(false); // Hide the Text UI element
        clusterNameText.transform.localScale = Vector3.one;

        // Reset to the original color
        if (rend != null)
        {
            rend.material.color = originalColor;
        }
    }

    private void OnMouseDown()
    {
        ClickCluster();

        starsVisible = !starsVisible;

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(starsVisible); //every click, either hide or show all stars
        }
    }

    private void ClickCluster()
    {
        infoPanel.SetActive(false);

        clusterNameText.gameObject.SetActive(false); // Hide the Text UI element
        clusterNameText.transform.localScale = Vector3.one;

        clicked = true;
    }
}
