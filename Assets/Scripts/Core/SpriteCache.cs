using System.Collections.Generic;
using UnityEngine;

//global sprite cache — wraps Resources.Load<Sprite> so each path is loaded at most once per session
//drop-in replacement: SpriteCache.Get(path) instead of Resources.Load<Sprite>(path)
public static class SpriteCache
{
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string path)
    {
        if (!cache.TryGetValue(path, out Sprite sprite))
        {
            sprite = Resources.Load<Sprite>(path);
            cache[path] = sprite;
        }
        return sprite;
    }

    //call this between scenes if you want to free memory (optional)
    public static void Clear() => cache.Clear();
}
