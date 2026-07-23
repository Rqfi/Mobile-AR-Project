using System;
using System.Collections.Generic;

[Serializable]
public class ProyekData
{
    public string id;
    public string nama;
    public string tanggal;
    public List<ScreenshotData> screenshots = new List<ScreenshotData>();
}