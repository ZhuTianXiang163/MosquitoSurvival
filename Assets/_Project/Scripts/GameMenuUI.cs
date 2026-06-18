using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Result UI")]
    [SerializeField] private TMP_Text resultCoinText;

    [Header("Settings UI")]
    [SerializeField] private Slider volumeSlider;

    private bool isPaused = false;

    // 记录“打开说明/设置之前”是哪个面板在显示
    private GameObject previousPanelBeforePopup;

    private void Start()
    {
        Time.timeScale = 0f;

        HideAllMenuPanels();

        if (startPanel != null)
        {
            startPanel.SetActive(true);
        }

        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied += ShowDeathPanel;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied -= ShowDeathPanel;
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscape();
        }
    }

    private void HandleEscape()
    {
        // 开始界面时，不用 Esc 打开暂停
        if (startPanel != null && startPanel.activeSelf)
        {
            return;
        }

        // 死亡 / 结算界面时，不处理 Esc
        if ((deathPanel != null && deathPanel.activeSelf) ||
            (resultPanel != null && resultPanel.activeSelf))
        {
            return;
        }

        // 如果当前在玩法说明界面，Esc 返回上一层
        if (instructionPanel != null && instructionPanel.activeSelf)
        {
            HideInstructions();
            return;
        }

        // 如果当前在设置界面，Esc 返回上一层
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            HideSettings();
            return;
        }

        // 正常切换暂停
        TogglePause();
    }

    private void HideAllMenuPanels()
    {
        if (startPanel != null) startPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (deathPanel != null) deathPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private GameObject GetCurrentOpenPanel()
    {
        if (startPanel != null && startPanel.activeSelf) return startPanel;
        if (pausePanel != null && pausePanel.activeSelf) return pausePanel;
        if (settingsPanel != null && settingsPanel.activeSelf) return settingsPanel;
        if (instructionPanel != null && instructionPanel.activeSelf) return instructionPanel;
        if (deathPanel != null && deathPanel.activeSelf) return deathPanel;
        if (resultPanel != null && resultPanel.activeSelf) return resultPanel;

        return null;
    }

    public void StartGame()
    {
        HideAllMenuPanels();

        previousPanelBeforePopup = null;
        isPaused = false;
        Time.timeScale = 1f;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            HideAllMenuPanels();

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            Time.timeScale = 0f;
        }
        else
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (instructionPanel != null)
            {
                instructionPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            previousPanelBeforePopup = null;
            Time.timeScale = 1f;
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        previousPanelBeforePopup = null;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (instructionPanel != null) instructionPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ShowInstructions()
    {
        previousPanelBeforePopup = GetCurrentOpenPanel();

        // 如果当前就是 instructionPanel，自然不用记录自己
        if (previousPanelBeforePopup == instructionPanel)
        {
            previousPanelBeforePopup = null;
        }

        HideAllMenuPanels();

        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void HideInstructions()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }

        if (previousPanelBeforePopup != null)
        {
            previousPanelBeforePopup.SetActive(true);
        }

        previousPanelBeforePopup = null;
    }

    public void ShowSettings()
    {
        previousPanelBeforePopup = GetCurrentOpenPanel();

        if (previousPanelBeforePopup == settingsPanel)
        {
            previousPanelBeforePopup = null;
        }

        HideAllMenuPanels();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void HideSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (previousPanelBeforePopup != null)
        {
            previousPanelBeforePopup.SetActive(true);
        }

        previousPanelBeforePopup = null;
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void ShowDeathPanel()
    {
        HideAllMenuPanels();

        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        previousPanelBeforePopup = null;
        isPaused = false;
        Time.timeScale = 0f;
    }

    public void ShowResultPanel()
    {
        HideAllMenuPanels();

        if (resultCoinText != null && GameManager.Instance != null)
        {
            resultCoinText.text = "Coins: " + GameManager.Instance.Coins;
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        previousPanelBeforePopup = null;
        isPaused = false;
        Time.timeScale = 0f;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
