using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using TMPro;

public class WorldMenuManager : MonoBehaviour
{
    public Transform worldListPanel;
    public GameObject worldListItemPrefab;
    public TMP_InputField inputWorldName;
    public Button createWorldButton;

    private List<string> savedWorlds = new List<string>();

    void Start()
    {
        Debug.Log("Starting WorldMenuManager...");

        if (worldListPanel == null)
            Debug.LogError("worldListPanel is not assigned.");
        if (worldListItemPrefab == null)
            Debug.LogError("worldListItemPrefab is not assigned.");
        if (inputWorldName == null)
            Debug.LogError("inputWorldName is not assigned.");
        if (createWorldButton == null)
            Debug.LogError("createWorldButton is not assigned.");

        LoadSavedWorlds();
        createWorldButton.onClick.AddListener(() =>
        {
            Debug.Log("Create World Button Clicked");
            CreateWorld();
        });
    }

    void LoadSavedWorlds()
    {
        Debug.Log("Loading saved worlds...");
        savedWorlds.Clear();

        string path = SaveSystem.GetSaveDirectory();
        if (Directory.Exists(path))
        {
            var dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs)
            {
                string worldName = Path.GetFileName(dir);
                savedWorlds.Add(worldName);
                Debug.Log("Found world: " + worldName);
            }
        }
        else
        {
            Debug.LogWarning("Save directory does not exist: " + path);
        }

        RefreshWorldList();
    }

    void RefreshWorldList()
    {
        Debug.Log("Refreshing world list...");
        if (worldListPanel == null)
        {
            Debug.LogError("worldListPanel is null.");
            return;
        }

        foreach (Transform child in worldListPanel)
            Destroy(child.gameObject);

        foreach (var world in savedWorlds)
        {
            if (worldListItemPrefab == null)
            {
                Debug.LogError("worldListItemPrefab is null.");
                return;
            }

            GameObject item = Instantiate(worldListItemPrefab, worldListPanel);
            Debug.Log("Instantiated world list item: " + item.name);

            TextMeshProUGUI worldNameText = item.transform.Find("WorldNameText")?.GetComponent<TextMeshProUGUI>();
            if (worldNameText != null)
            {
                worldNameText.text = world;
                Debug.Log("Set world name text: " + world);
            }
            else
            {
                Debug.LogError("WorldNameText component not found in worldListItemPrefab");
            }

            Button deleteButton = item.transform.Find("DeleteButton")?.GetComponent<Button>();
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() =>
                {
                    Debug.Log("Delete Button Clicked for: " + world);
                    DeleteWorld(world);
                });
            }
            else
            {
                Debug.LogError("DeleteButton component not found in worldListItemPrefab");
            }

            Button loadButton = item.transform.Find("LoadButton")?.GetComponent<Button>();
            if (loadButton != null)
            {
                loadButton.onClick.AddListener(() =>
                {
                    Debug.Log("Load Button Clicked for: " + world);
                    LoadWorld(world);
                });
            }
            else
            {
                Debug.LogError("LoadButton component not found in worldListitemprefab");
            }
        }
    }

    void CreateWorld()
    {
        string newWorldName = inputWorldName.text.Trim();
        if (string.IsNullOrEmpty(newWorldName))
        {
            Debug.LogWarning("World name is empty");
            return;
        }

        string newWorldPath = Path.Combine(SaveSystem.GetSaveDirectory(), newWorldName);
        if (!Directory.Exists(newWorldPath))
        {
            Directory.CreateDirectory(newWorldPath);
            Debug.Log("Created new world directory: " + newWorldPath);
        }
        else
        {
            Debug.LogWarning("World directory already exists: " + newWorldPath);
        }

        savedWorlds.Add(newWorldName);
        RefreshWorldList();
    }

    void DeleteWorld(string worldName)
    {
        Debug.Log("Deleting world: " + worldName);
        string path = Path.Combine(SaveSystem.GetSaveDirectory(), worldName);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            Debug.Log("Deleted world directory: " + path);
        }
        else
        {
            Debug.LogWarning("World directory does not exist: " + path);
        }
        savedWorlds.Remove(worldName);
        RefreshWorldList();
    }

    void LoadWorld(string worldName)
    {
        Debug.Log("Loading world: " + worldName);
        PlayerPrefs.SetString("SelectedWorld", worldName);
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }
}
