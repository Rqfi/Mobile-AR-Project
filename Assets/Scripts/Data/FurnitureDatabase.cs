using System.Collections.Generic;

public static class FurnitureDatabase
{
    private static List<FurnitureItem> _items = new List<FurnitureItem>();

    // Mengisi cache dari data Firebase yang telah diunduh
    public static void SetItems(List<FurnitureItem> items)
    {
        _items = items;
    }

    public static List<FurnitureItem> GetAll() => _items;

    public static List<FurnitureItem> GetByCategory(string category)
    {
        if (category == "Semua") return _items;
        return _items.FindAll(item => item.category == category);
    }
}
