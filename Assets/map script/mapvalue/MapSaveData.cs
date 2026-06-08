using System.Collections.Generic;
using System.IO;
using UnityEngine;


[System.Serializable]
public class MapSaveData
{
    public string mapName;
    public int width;
    public int height;
    public List<MapObjectData> objects;
}