public interface IStaticInventoryItem
{
    string Id { get; }

    int Width { get; }

    int Height { get; }
}

public enum EquipmentSlotType
{
    None = 0,
    Head = 1,
    Vest = 2,
    Shirt = 3,
    Pants = 4,
    Socks = 5,
    Boots = 6,
    MainHand = 7,
    OffHand = 8
}
