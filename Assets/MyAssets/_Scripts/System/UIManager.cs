using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Singleton instance
    public static UIManager Instance { get; private set; }

    [SerializeField] private HealthSystem _playerHealthSystem;

    // canvas vars
    [SerializeField] private Canvas _uICanvas;
    [SerializeField] private Canvas _inventoryCanvas;
    [SerializeField] private Canvas _pauseMenuCanvas;
    [SerializeField] private Canvas _deathScreenCanvas;

    // element vars
    private TMP_Text _healthText;

    // general vars
    private bool _isGamePaused = false;
    private bool _isInventoryOpen = false;

    private void Awake()
    {
        // Set the static reference when this object is created
        Instance = this;

        // cache the health text once, then only update its value in Update
        _healthText = _uICanvas.GetComponentInChildren<TMP_Text>();
    }

    private void Update()
    {
        // refresh the health display every frame
        _healthText.text = $"Health: {_playerHealthSystem.CurrentHealth}/{_playerHealthSystem.HealthPoints}";
        TogglePause();
        ToggleInventory();
    }

    #region UI Menus
    public void TogglePause()
    {
        // ignore frames where pause was not pressed
        if (!InputManager.PauseWasPressed)
        {
            return;
        }

        SetPauseState(!_isGamePaused);
    }

    private void SetPauseState(bool shouldPause)
    {
        _isGamePaused = shouldPause;
        _pauseMenuCanvas.gameObject.SetActive(_isGamePaused);

        // switch input maps so pause can be triggered from either state
        if (_isGamePaused)
        {
            InputManager.PlayerInput.SwitchCurrentActionMap("UI");
        }
        else
        {
            InputManager.PlayerInput.SwitchCurrentActionMap("Player");
        }

        // pause uses an infinite wait, unpause returns immediately
        TimeManager.Instance.AdjustGlobalTime(_isGamePaused ? 0f : 1f, _isGamePaused ? Mathf.Infinity : 0f, true);
    }

    public void SetDeathState(bool shouldTurnOnDeathScreen)
    {
        if (shouldTurnOnDeathScreen) {
            _deathScreenCanvas.gameObject.SetActive(true);
            // pause the game when the death screen is shown
            InputManager.PlayerInput.SwitchCurrentActionMap("UI");
            TimeManager.Instance.AdjustGlobalTime(0f, Mathf.Infinity, true);
        } else { 
            _deathScreenCanvas.gameObject.SetActive(false);
            InputManager.PlayerInput.SwitchCurrentActionMap("Player");
        }
    }

    public void ToggleInventory()
    {
        // ignore frames where pause was not pressed
        if (!InputManager.InventoryWasPressed)
        {
            return;
        }

        SetInventoryState(!_isInventoryOpen);
    }

    private void SetInventoryState(bool shouldOpen)
    {
        _isInventoryOpen = shouldOpen;
        _inventoryCanvas.gameObject.SetActive(_isInventoryOpen);
        // switch input maps so inventory can be triggered from either state
        if (_isInventoryOpen)
        {
            InputManager.PlayerInput.SwitchCurrentActionMap("UI");
        }
        else
        {
            InputManager.PlayerInput.SwitchCurrentActionMap("Player");
        }
        // pause uses an infinite wait, unpause returns immediately
        TimeManager.Instance.AdjustGlobalTime(_isInventoryOpen ? 0f : 1f, _isInventoryOpen ? Mathf.Infinity : 0f, true);
    }

    #endregion

    #region Button Handlers

    public void OnResumeButtonClick()
    {
        SetPauseState(false);
    }

    public void OnRestartGameButtonClick()
    {
        // unpause the game if it was paused, then reload the current scene
        SetPauseState(false);
        SetDeathState(false);
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void OnQuitGameButtonClick()
    {
        Application.Quit();
    }

    public void OnFVButtonClick()
    {

    }

    public void OnProteinButtonClick()
    {

    }

    public void OnCarbsButtonClick()
    {

    }

    public void OnDairyButtonClick()
    {

    }

    public void OnFatsOilsButtonClick()
    {

    }

    public void OnSeasoningsButtonClick()
    {

    }

    #endregion
}
