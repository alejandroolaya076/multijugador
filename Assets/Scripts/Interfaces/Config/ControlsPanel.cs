using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;



public class ControlsPanel : MonoBehaviour
{
    // ---------------------------------------------------------
    // Definición de acciones
    // ---------------------------------------------------------
    public enum GameAction
    {
        Crouch,
        MoveLeft,
        MoveRight,
        Jump,
        Dash,
        Attack
    }

    // ---------------------------------------------------------
    // Clase que representa una fila del panel
    // ---------------------------------------------------------
    [System.Serializable]
    public class KeyBindingRow
    {
        public GameAction action;
        public Button     button;       // El botón clickeable
        public TMP_Text   buttonLabel;  // Texto dentro del botón (ej: "W")
    }

    // ---------------------------------------------------------
    // Inspector
    // ---------------------------------------------------------
    [Header("Filas del panel (una por acción)")]
    public List<KeyBindingRow> rows = new List<KeyBindingRow>();

    [Header("UI de feedback")]
    public GameObject listeningOverlay;   // Panel semitransparente opcional: "Presiona una tecla..."
    public TMP_Text   listeningText;      // Texto que dice qué acción está esperando

    [Header("Botones del pie")]
    public Button saveButton;
    public Button defaultButton;
    public Button exitButton;

    // ---------------------------------------------------------
    // Estado interno
    // ---------------------------------------------------------

    // Teclas por defecto
    private static readonly Dictionary<GameAction, KeyCode> _defaults = new()
    {

        { GameAction.Crouch,  KeyCode.S },
        { GameAction.MoveLeft,  KeyCode.A },
        { GameAction.MoveRight, KeyCode.D },
        { GameAction.Jump,      KeyCode.Space },
        { GameAction.Dash,    KeyCode.K },
        { GameAction.Attack,  KeyCode.J },
    };

    // Bindings actuales (se cargan de PlayerPrefs o se usan los defaults)
    private Dictionary<GameAction, KeyCode> _bindings = new();

    // Acción que está esperando input (-1 = ninguna)
    private GameAction? _listeningAction = null;
    private KeyBindingRow _listeningRow  = null;

    // Teclas que NO se pueden asignar
    private static readonly HashSet<KeyCode> _blacklist = new()
    {
        KeyCode.Escape,
        KeyCode.Mouse0,
        KeyCode.Mouse1,
    };

    // =========================================================
    // Unity lifecycle
    // =========================================================

    private void Awake()
    {
        LoadBindings();
        SetupButtons();
        RefreshAllLabels();
        SetListeningUI(false);
    }

    private void Update()
    {
        if (_listeningAction == null) return;

        // Cancelar con Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelListening();
            return;
        }

        // Detectar cualquier tecla presionada
        foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(kc))       continue;
            if (_blacklist.Contains(kc))     continue;
            if (kc >= KeyCode.JoystickButton0) continue; // ignorar joystick

            AssignKey(_listeningAction.Value, kc);
            return;
        }
    }

    // =========================================================
    // Setup
    // =========================================================

    private void SetupButtons()
    {
        foreach (var row in rows)
        {
            // Captura local para el closure del lambda
            var capturedRow = row;
            row.button.onClick.AddListener(() => OnKeyButtonClicked(capturedRow));
        }

        saveButton.onClick.AddListener(SaveBindings);
        defaultButton.onClick.AddListener(ResetToDefaults);
        exitButton.onClick.AddListener(OnExit);
    }

    // =========================================================
    // Lógica de escucha
    // =========================================================

    private void OnKeyButtonClicked(KeyBindingRow row)
    {
        // Si ya estábamos escuchando otra acción, cancelamos primero
        if (_listeningAction != null)
            CancelListening();

        _listeningAction = row.action;
        _listeningRow    = row;

        // Feedback visual en el botón
        row.buttonLabel.text = "...";
        row.buttonLabel.color =  Color.white;

        SetListeningUI(true, row.action);
    }

    private void AssignKey(GameAction action, KeyCode newKey)
    {
        // Verificar que la tecla no esté ya asignada a otra acción
        foreach (var kvp in _bindings)
        {
            if (kvp.Key != action && kvp.Value == newKey)
            {
                // Intercambio: la otra acción recibe la tecla que tenía esta
                KeyCode oldKey = _bindings[action];
                _bindings[kvp.Key] = oldKey;
                RefreshLabel(kvp.Key);
                break;
            }
        }

        _bindings[action] = newKey;
        RefreshLabel(action);

        _listeningAction = null;
        _listeningRow    = null;
        SetListeningUI(false);
    }

    private void CancelListening()
    {
        if (_listeningRow != null)
            RefreshLabel(_listeningRow.action); // restaura el texto anterior

        _listeningAction = null;
        _listeningRow    = null;
        SetListeningUI(false);
    }

    // =========================================================
    // UI helpers
    // =========================================================

    private void SetListeningUI(bool active, GameAction? action = null)
    {
        if (listeningOverlay != null)
            listeningOverlay.SetActive(active);

        if (active && action != null && listeningText != null)
            listeningText.text = $"Presiona una tecla para:\n<b>{ActionName(action.Value)}</b>\n\n<size=70%>ESC para cancelar</size>";
    }

    private void RefreshLabel(GameAction action)
    {
        var row = rows.Find(r => r.action == action);
        if (row == null) return;

        row.buttonLabel.text  = KeyDisplayName(_bindings[action]);
        row.buttonLabel.color = Color.white;
    }

    private void RefreshAllLabels()
    {
        foreach (var row in rows)
            RefreshLabel(row.action);
    }

    // =========================================================
    // Guardar / Cargar / Resetear
    // =========================================================

    public void SaveBindings()
    {
        foreach (var kvp in _bindings)
            PlayerPrefs.SetInt("key_" + kvp.Key.ToString(), (int)kvp.Value);

        PlayerPrefs.Save();
        Debug.Log("[ControlsPanel] Bindings guardados.");
    }

    private void LoadBindings()
    {
        _bindings.Clear();

        foreach (var kvp in _defaults)
        {
            string prefKey = "key_" + kvp.Key.ToString();

            if (PlayerPrefs.HasKey(prefKey))
                _bindings[kvp.Key] = (KeyCode)PlayerPrefs.GetInt(prefKey);
            else
                _bindings[kvp.Key] = kvp.Value;
        }
    }

    public void ResetToDefaults()
    {
        foreach (var kvp in _defaults)
            _bindings[kvp.Key] = kvp.Value;

        RefreshAllLabels();
        Debug.Log("[ControlsPanel] Bindings reseteados a valores por defecto.");
    }

    private void OnExit()
    {
        // Recarga los bindings guardados (descarta cambios no guardados)
        LoadBindings();
        RefreshAllLabels();
        SceneManager.LoadScene("Menu");
    }

    // =========================================================
    // Acceso público (para otros sistemas, ej: el Player)
    // =========================================================

    /// <summary>
    /// Devuelve la KeyCode asignada a una acción.
    /// Uso: if (Input.GetKeyDown(ControlsPanel.Instance.GetKey(GameAction.Jump)))
    /// </summary>
    public KeyCode GetKey(GameAction action)
    {
        return _bindings.TryGetValue(action, out KeyCode kc) ? kc : _defaults[action];
    }

    // Singleton opcional (quitar si usas otro sistema de referencias)
    public static ControlsPanel Instance { get; private set; }
    private void OnEnable()  { if (Instance == null) Instance = this; }
    private void OnDisable() { if (Instance == this) Instance = null; }

    // =========================================================
    // Utilidades de texto
    // =========================================================

    private static string KeyDisplayName(KeyCode kc) => kc switch
    {
        KeyCode.Space      => "SPACE",
        KeyCode.LeftShift  => "L-SHIFT",
        KeyCode.RightShift => "R-SHIFT",
        KeyCode.LeftControl=> "L-CTRL",
        KeyCode.RightControl=>"R-CTRL",
        KeyCode.LeftAlt    => "L-ALT",
        KeyCode.RightAlt   => "R-ALT",
        KeyCode.Return     => "ENTER",
        KeyCode.Backspace  => "BKSP",
        KeyCode.Tab        => "TAB",
        KeyCode.UpArrow    => "↑",
        KeyCode.DownArrow  => "↓",
        KeyCode.LeftArrow  => "←",
        KeyCode.RightArrow => "→",
        _                  => kc.ToString().Replace("Alpha", "").Replace("Keypad", "KP ").ToUpper()
    };

    private static string ActionName(GameAction action) => action switch
    {
        GameAction.Crouch  => "Mover Abajo",
        GameAction.MoveLeft  => "Mover Izquierda",
        GameAction.MoveRight => "Mover Derecha",
        GameAction.Jump      => "Saltar",
        GameAction.Dash    => "Dashear",
        GameAction.Attack  => "Golpear",
        _                    => action.ToString()
    };
}
