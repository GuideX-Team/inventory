using System;
using UnityEngine;

public sealed class TetrisInventory : Inventory<InventoryItem>
{
    public event Action<TetrisInventory> OnInventoryChanged;

    // When true, any item placed into this inventory is treated as 1x1
    public bool TreatAllItemsAsUnit { get; set; }

    public TetrisInventory()
    {
        Ctor();
    }

    public TetrisInventory(Vector2Int gridSize)
        : base(gridSize)
    {
        Ctor();
    }

    public TetrisInventory(int width, int height)
        : base(width, height)
    {
        Ctor();
    }

    private void Ctor()
    {
        CreateItemFunc = () => new InventoryItem();

        CreateBackpackItemFunc = (gridSize) =>
        {
            return new BackpackInventoryItem { Inventory = new TetrisInventory(gridSize) };
        };

        OnCollectionChanged += Inventory_OnCollectionChanged;
    }

    private void Inventory_OnCollectionChanged(Inventory<InventoryItem> inventory)
    {
        OnInventoryChanged?.Invoke(this);
    }

    protected override void GetItemSizeForPlacement(IStaticInventoryItem item, bool rotated, out int width, out int height)
    {
        if (TreatAllItemsAsUnit)
        {
            width = 1;
            height = 1;
            return;
        }

        base.GetItemSizeForPlacement(item, rotated, out width, out height);
    }
}
