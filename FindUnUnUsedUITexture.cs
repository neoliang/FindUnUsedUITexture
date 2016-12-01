using UnityEngine;
using UnityEditor;

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using System;

public static class LinqHelper {
    public static TSource Fold<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func, TSource id)
    {
        TSource r = id;
        foreach (var s in source)
        {
            r = func(r, s);
        }
        return r;
    }
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (T element in source)
            action(element);
    }
    public static IEnumerable<U> SelectI<U, T>(this IEnumerable<T> source, Func<T, int, U> action)
    {
        int i = 0;
        foreach (var s in source)
        {
            yield return action(s, i);
            i += 1;
        }
    }
    public static TSource Reduce<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func) where TSource : new()
    {
        return Fold<TSource>(source, func, new TSource());
    }
    public static void ForEachI<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        int i = 0;
        foreach (T element in source)
        {
            action(element, i);
            i += 1;
        }

    }
}
public static class FindUnUnUsedUITexture
{


    static List<string> getUUIDsInFile(string path)
    {
        StreamReader file = new StreamReader(path);
        List<string> uuids = new List<string>();
        string line;
        while ((line = file.ReadLine()) != null)
        {
            var reg = new Regex(@"([a-f0-9]{32})");
            var m = reg.Match(line);
            if (m.Success)
            {
                uuids.Add(m.Groups[0].Value);
            }
        }
        file.Close();
        return uuids;
    }
    // Use this for initialization
    [MenuItem("Tools/UI冗余图片扫描")]
    public static void Scan()
    {

        var uiPrefabRootDir = EditorUtility.OpenFolderPanel("选择UIPrefab目录",  "Assets","");
        if (string.IsNullOrEmpty(uiPrefabRootDir))
        {
            return;
        }

        var uiPicRootDir = EditorUtility.OpenFolderPanel("选择UIPrefab目录", "Assets", "");
        if (string.IsNullOrEmpty(uiPicRootDir))
        {
            return;
        }
        
        //find all meta and pic path
        var uuidReg = new Regex(@"guid: ([a-f0-9]{32})");
        var pngs = Directory.GetFiles(uiPicRootDir, "*.meta", SearchOption.AllDirectories)
        .Select(p => "Assets/" + p.Replace('\\','/').Substring(Application.dataPath.Length+1))
        .Where(p =>
        {
            return p.EndsWith(".png.meta") || p.EndsWith(".jpg.meta") || p.EndsWith(".tag.meta");
        }).ToList();
        var uuid2path = new Dictionary<string, string>();
        pngs.ForEachI((png, i) =>
        {
            var matcher = uuidReg.Match(File.ReadAllText(png));
            var uuid = matcher.Groups[1].Value;
            if (uuid2path.ContainsKey(uuid))
            {
                Debug.LogError("uuid dup" + uuid + " \n" + png + "\n" + uuid2path[uuid]);
            }
            else
            {
                uuid2path.Add(uuid, png.Substring(0,png.Length-5));
            }
            EditorUtility.DisplayProgressBar("扫描图片中", png, (float)i / pngs.Count);

        });

        //find all prefab and search pic uuid
        var prefabs = Directory.GetFiles(uiPrefabRootDir, "*.prefab", SearchOption.AllDirectories);
        var anims = Directory.GetFiles("Assets/", "*.anim", SearchOption.AllDirectories).Where(p => !p.Replace('\\', '/').Contains("Characters/"));
        var allFiles = prefabs.Concat(anims).ToList();
        var alluuids = allFiles
        .SelectI((f, i) => {
            EditorUtility.DisplayProgressBar("获取引用关系", f, (float)i / allFiles.Count);
            return getUUIDsInFile(f);
        }).ToList().Aggregate((a, b) => a.Concat(b).ToList()).ToList();
        EditorUtility.ClearProgressBar();
        //rm used pic uuid
        var uuidshashset = new HashSet<string>(alluuids);
        var em = uuidshashset.GetEnumerator();
        while(em.MoveNext())
        {
            var uuid = em.Current;
            uuid2path.Remove(uuid);
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("UnUsedFiles: ");
        sb.Append(uuid2path.Count);
        sb.Append("\n");
        uuid2path.ForEach(kv => sb.Append(kv.Value +"\n"));

        File.WriteAllText("Assets/unusedpic.txt", sb.ToString());
        EditorUtility.DisplayDialog("扫描成功", string.Format("共找到{0}个冗余图片\n请在Assets/unsedpic.txt查看结果",uuid2path.Count), "ok");
    }
}
