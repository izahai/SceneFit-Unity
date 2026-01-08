using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FloatingPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button nextButton;
    [SerializeField] private Text panelText;
    [SerializeField] private TextMeshProUGUI panelTextTMP;

    [Header("Input Keys")]
    [SerializeField] private KeyCode toggleKey = KeyCode.M;
    [SerializeField] private KeyCode nextKey = KeyCode.R;

    [Header("Panel Texts")]
    [SerializeField] private string[] texts = new string[] { "Baseline 1", "Baseline 2", "Baseline 3" };

    private int currentIndex;

    private void Awake()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(ShowNextText);
        }

        UpdateText();
        SetPanelVisible(panelRoot == null || panelRoot.activeSelf);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }

        if (Input.GetKeyDown(nextKey))
        {
            if (nextButton != null)
            {
                nextButton.onClick.Invoke();
            }
            else
            {
                ShowNextText();
            }
        }
    }

    public void TogglePanel()
    {
        if (panelRoot == null)
        {
            return;
        }

        SetPanelVisible(!panelRoot.activeSelf);
    }

    public void ShowNextText()
    {
        if (texts == null || texts.Length == 0)
        {
            return;
        }

        currentIndex = (currentIndex + 1) % texts.Length;
        UpdateText();
    }

    private void UpdateText()
    {
        if (texts == null || texts.Length == 0)
        {
            return;
        }

        string message = texts[Mathf.Clamp(currentIndex, 0, texts.Length - 1)];

        if (panelTextTMP != null)
        {
            panelTextTMP.text = message;
            return;
        }

        if (panelText != null)
        {
            panelText.text = message;
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
        }
    }
}
