using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{


    [SerializeField] private HealthSystem _playerHealthSystem;

    // canvas vars
    [SerializeField] private Canvas _uICanvas;
    [SerializeField] private Canvas _inventoryCanvas;
    [SerializeField] private Canvas _pauseMenuCanvas;

    // element vars
    private TMP_Text _healthText;

    private void Awake()
    { 
        _healthText = _uICanvas.GetComponentInChildren<TMP_Text>();
    }
    private void Update()
    {
        _healthText.text = $"Health: {_playerHealthSystem.CurrentHealth}/{_playerHealthSystem.HealthPoints}";

    }

}
