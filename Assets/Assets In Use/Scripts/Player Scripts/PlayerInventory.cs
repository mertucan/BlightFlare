using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInventory : MonoBehaviour
{
    public int keys { get; private set; }
    public int pennies { get; private set; }

    private HeartUI _heartUI;
    public static PlayerInventory instance;
    private bool _initialized = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // ← bunu ekle

        if (!_initialized)
        {
            keys = 1;
            pennies = 1;
            _initialized = true;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        _heartUI = FindFirstObjectByType<HeartUI>(FindObjectsInactive.Include);
        if (_heartUI != null)
        {
            _heartUI.UpdateKey(keys);
            _heartUI.UpdatePennies(pennies);
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        RefreshUI();
    }

    public void AddKey(int amount = 1)
    {
        keys += amount;
        _heartUI?.UpdateKey(keys);
    }

    public void AddPenny(int amount = 1)
    {
        pennies = Mathf.Max(0, pennies + amount);
        _heartUI?.UpdatePennies(pennies);
    }

    public bool UseKey()
    {
        if (keys <= 0) return false;
        keys--;
        _heartUI?.UpdateKey(keys);
        return true;
    }
}