using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text.RegularExpressions;

public class TextureAtlasManager : MonoBehaviour
{
    public Texture2D atlasTexture;
    public TextAsset atlasJson;
    public int atlasWidth = 1152;
    public int atlasHeight = 3168;
    public static TextureAtlasManager instance;


    private Dictionary<string, Rect> uvRects;

    void Awake()
    {
        instance = this;
        LoadAtlas();
    }

    void LoadAtlas()
    {
        uvRects = new Dictionary<string, Rect>();

        // Parse JSON à la main (car JsonUtility ne gère pas les dictionnaires imbriqués)
        string json = atlasJson.text;
        var matches = Regex.Matches(json, "\"([^\"]+)\"\\s*:\\s*\\{\\s*\"frame\"\\s*:\\s*\\{\\s*\"x\":(\\d+),\"y\":(\\d+),\"w\":(\\d+),\"h\":(\\d+)");
        foreach (Match match in matches)
        {
            string name = match.Groups[1].Value;
            int x = int.Parse(match.Groups[2].Value);
            int y = int.Parse(match.Groups[3].Value);
            int w = int.Parse(match.Groups[4].Value);
            int h = int.Parse(match.Groups[5].Value);

            // Attention à l'origine des UV (Unity: bas-gauche, TexturePacker: haut-gauche)
            float uvX = x / (float)atlasWidth;
            float uvY = 1f - ((y + h) / (float)atlasHeight);
            float uvW = w / (float)atlasWidth;
            float uvH = h / (float)atlasHeight;
            uvRects[name] = new Rect(uvX, uvY, uvW, uvH);
        }
    }

    public Rect GetUV(string name)
    {
        if (uvRects.TryGetValue(name, out Rect rect))
            return rect;
        else
            return new Rect(0, 0, 1, 1); // fallback
    }
}