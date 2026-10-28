[System.Serializable]
public class FurnitureItem
{
    public string id;
    public string name;
    public string category;
    public string description;
    public float width;
    public float depth;
    public float height;
    public float scale;
    public string thumbnailUrl;
    public string modelUrl;
    public PlacementType placementType = PlacementType.Floor;
}
public enum PlacementType
{
    Floor,
    Wall,
    Tabletop,
    FloorOrWall
}