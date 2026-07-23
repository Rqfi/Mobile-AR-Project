using System.Collections.Generic;

public static class FurnitureDatabase
{
    private static List<FurnitureItem> _items = new List<FurnitureItem>
    {
        new FurnitureItem { id="F001", name="Meja Makan",    category="Meja",   description="Meja makan minimalis untuk 4 orang.", width=120, depth=80, height=75 },
        new FurnitureItem { id="F002", name="Meja Kerja",    category="Meja",   description="Meja kerja ergonomis dengan laci.",    width=140, depth=70, height=76 },
        new FurnitureItem { id="F003", name="Meja Kopi",     category="Meja",   description="Meja kopi bundar kayu solid.",         width=80,  depth=80, height=45 },
        new FurnitureItem { id="F004", name="Meja Konsol",   category="Meja",   description="Meja konsol ramping untuk lorong.",    width=100, depth=35, height=80 },
        new FurnitureItem { id="F005", name="Kursi Makan",   category="Kursi",  description="Kursi makan dengan sandaran kayu.",    width=45,  depth=50, height=90 },
        new FurnitureItem { id="F006", name="Kursi Kerja",   category="Kursi",  description="Kursi kerja dengan roda dan armrest.", width=65,  depth=65, height=110 },
        new FurnitureItem { id="F007", name="Sofa 2 Dudukan",category="Sofa",   description="Sofa dua dudukan bahan premium.",     width=150, depth=85, height=85 },
        new FurnitureItem { id="F008", name="Sofa 3 Dudukan",category="Sofa",   description="Sofa tiga dudukan ruang keluarga.",   width=220, depth=90, height=85 },
        new FurnitureItem { id="F009", name="Lemari Pakaian",category="Lemari", description="Lemari pakaian 3 pintu geser.",       width=180, depth=60, height=200 },
        new FurnitureItem { id="F010", name="Lemari Buku",   category="Lemari", description="Rak buku 5 tingkat kayu jati.",       width=90,  depth=30, height=180 },
    };

    public static List<FurnitureItem> GetAll() => _items;

    public static List<FurnitureItem> GetByCategory(string category)
    {
        if (category == "Semua") return _items;
        return _items.FindAll(item => item.category == category);
    }
}