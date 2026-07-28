using System;
using System.Collections.Generic;

[Serializable]
public class ProyekData
{
    public string id;
    public string nama;
    public string tanggal;
    public string thumbnailPath;
    public List<ScreenshotData> screenshots = new List<ScreenshotData>();
}