using System.Collections.Generic;
using System.Linq;
using Kukumberman.SaveSystem;
using UnityEngine;
using UnityEngine.Events;

public sealed class InventoryManager : MonoBehaviour
{
    private const string kSaveKey = "inventory.json";

    public static InventoryManager Singleton { get; private set; }

    [SerializeField]
    private UnityEvent<IDynamicInventoryItem> _onItemRemoved;

    [SerializeField]
    private InventoryView _view;

    [SerializeField]
    private Vector2Int _gridSize;

    [SerializeField]
    private InventoryItemCollectionSO _itemCollection;

    [SerializeField]
    private bool _loadOnStart;

    private TetrisInventory _inventory;
    private Dictionary<string, TetrisInventory> _equipmentInventories = new();

    private ISaveSystem _saveSystem;
    private ISerialization _serialization;

    public TetrisInventory RootInventory => _inventory;
    public InventoryItemCollectionSO ItemCollection => _itemCollection;
    public IReadOnlyDictionary<string, TetrisInventory> EquipmentInventories => _equipmentInventories;

    private void Awake()
    {
        Singleton = this;
    }

    private void OnDestroy()
    {
        Singleton = null;
    }

    private void Start()
    {
        _saveSystem = GetSaveSystem();
        _serialization = GetSerialization();

        _inventory = new TetrisInventory(_gridSize);

        _view.CreateGrid(_gridSize);
        _view.BindAndSync(_inventory);
        // todo: temp solution, it should be null since it is referenced as "destinationInventoryId" and compared to null is this class
        _view.Stash.DynamicId = null;

        if (_loadOnStart)
        {
            Load();
        }

        InitializeEquipmentInventories();
        _view.BindEquipment(_equipmentInventories);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddDebugItem();
        }
    }

    public InventoryItemSO GetStaticItemById(string id)
    {
        return _itemCollection.Items.FirstOrDefault(item => item.Id == id);
    }

    public IDynamicInventoryItem GetDynamicItemById(string id)
    {
        return GetDynamicItemById(id, out var _);
    }

    public IDynamicInventoryItem GetDynamicItemById(string id, out TetrisInventory parentInventory)
    {
        // todo: search inner inventories (Breadth-first search) (needs testing)
        parentInventory = null;

        var itemInStash = _inventory.Items.Find(item => item.Id == id);

        if (itemInStash != null)
        {
            parentInventory = _inventory;
            return itemInStash;
        }

        var queue = new Queue<IDynamicBackpackInventoryItem>();

        for (int i = 0; i < _inventory.Items.Count; i++)
        {
            if (_inventory.Items[i] is IDynamicBackpackInventoryItem backpackItem)
            {
                queue.Enqueue(backpackItem);
            }
        }

        while (queue.Count > 0)
        {
            var backpackItem = queue.Dequeue();

            for (int i = 0; i < backpackItem.Inventory.Items.Count; i++)
            {
                var innerItem = backpackItem.Inventory.Items[i];

                if (innerItem.Id == id)
                {
                    parentInventory = backpackItem.Inventory as TetrisInventory;
                    return innerItem;
                }

                if (innerItem is IDynamicBackpackInventoryItem innerBackpackItem)
                {
                    queue.Enqueue(innerBackpackItem);
                }
            }
        }

        return null;
    }

    [ContextMenu(nameof(AddDebugItem))]
    private void AddDebugItem()
    {
        AddItem(_itemCollection.Items[0]);
    }

    public bool TryRemoveItem(string id)
    {
        var dynamicItem = GetDynamicItemById(id, out var parentInventory);

        if (dynamicItem == null)
        {
            return true;
        }

        if (parentInventory.RemoveItemById(id, out var removedItem))
        {
            _onItemRemoved.Invoke(removedItem);
            return true;
        }

        return false;
    }

    public bool TransferItemToInventory(string dynamicItemId, string destinationInventoryId)
    {
        var inventoryItem =
            GetDynamicItemById(dynamicItemId, out var parentInventory) as InventoryItem;

        if (inventoryItem == null)
        {
            return false;
        }

        TetrisInventory destinationInventory;

        if (destinationInventoryId != null)
        {
            var dynamicItemDropTarget = GetDynamicItemById(destinationInventoryId);

            if (
                dynamicItemDropTarget == null
                || dynamicItemDropTarget is not IDynamicBackpackInventoryItem backpackItem
            )
            {
                return false;
            }

            destinationInventory = backpackItem.Inventory as TetrisInventory;
        }
        else
        {
            destinationInventory = _inventory;
        }

        if (destinationInventory.Contains(inventoryItem))
        {
            Debug.LogWarning("can't transfer item from same inventory (ok?)");
            return false;
        }

        if (destinationInventory.AddExistingItem(inventoryItem))
        {
            if (parentInventory.RemoveItemById(inventoryItem.Id, out var _))
            {
                return true;
            }
            else
            {
                Debug.Log("this should never happen");
            }
        }

        return false;
    }

    public bool MoveItemToInventory(
        string dynamicItemId,
        string destinationInventoryId,
        Vector2Int gridPosition,
        bool rotated
    )
    {

        var inventoryItem =
            GetDynamicItemById(dynamicItemId, out var parentInventory) as InventoryItem;

        if (inventoryItem == null)
        {
            return false;
        }

        TetrisInventory destinationInventory;
    bool isEquipmentSlot = false;

        if (destinationInventoryId != null)
        {
            if (_equipmentInventories.TryGetValue(destinationInventoryId, out var equipmentInv))
            {
                destinationInventory = equipmentInv;
            isEquipmentSlot = true;

                var staticItem = inventoryItem.Item as InventoryItemSO;
                var slot = ParseSlotFromInventoryId(destinationInventoryId);
            var sizeOk = gridPosition == Vector2Int.zero; // equipment treats any item as 1x1
            var typeOk = staticItem != null && staticItem.IsEquippable && staticItem.EquipSlot == slot;

                if (!sizeOk || !typeOk)
                {
                    return false;
                }
            }
            else
            {
                var dynamicItemDropTarget = GetDynamicItemById(destinationInventoryId);

                if (
                    dynamicItemDropTarget == null
                    || dynamicItemDropTarget is not IDynamicBackpackInventoryItem backpackItem
                )
                {
                    return false;
                }

                if (inventoryItem == backpackItem)
                {
                    return false;
                }

                destinationInventory = backpackItem.Inventory as TetrisInventory;
            }
        }
        else
        {
            destinationInventory = _inventory;
        }

    // Если перемещаем в том же инвентаре - простое перемещение
        if (parentInventory == destinationInventory)
        {
            return parentInventory.MoveItemByIdTo(
                dynamicItemId,
                gridPosition.x,
                gridPosition.y,
                rotated
            );
        }
    else
    {
        // НОВАЯ ЛОГИКА: Проверяем, есть ли предмет в целевом слоте
        InventoryItem existingItem = null;

        // Для слотов экипировки (1x1)
        if (isEquipmentSlot && destinationInventory.Items.Count > 0)
        {
            existingItem = destinationInventory.Items[0] as InventoryItem;
        }
        // Для обычных инвентарей - проверяем область
        else if (!isEquipmentSlot)
        {
            var item = inventoryItem.Item;
            int width = !rotated ? item.Width : item.Height;
            int height = !rotated ? item.Height : item.Width;

            // Находим предмет в целевой позиции (учитываем поворот каждого предмета)
            for (int i = 0; i < destinationInventory.Items.Count; i++)
            {
                var checkItem = destinationInventory.Items[i];
                var checkPos = checkItem.GridPosition;

                var checkItemWidth = !checkItem.IsRotated ? checkItem.Item.Width : checkItem.Item.Height;
                var checkItemHeight = !checkItem.IsRotated ? checkItem.Item.Height : checkItem.Item.Width;

                // Проверяем пересечение прямоугольников
                if (gridPosition.x < checkPos.x + checkItemWidth &&
                    gridPosition.x + width > checkPos.x &&
                    gridPosition.y < checkPos.y + checkItemHeight &&
                    gridPosition.y + height > checkPos.y)
                {
                    existingItem = checkItem as InventoryItem;
                    break;
                }
            }
        }

        // SWAP LOGIC: Если в целевом слоте есть предмет - меняем местами
        if (existingItem != null)
        {
            // Сохраняем данные существующего предмета
            var existingPos = existingItem.GridPosition;
            var existingRotated = existingItem.IsRotated;

            // Удаляем оба предмета
            if (!destinationInventory.RemoveItemById(existingItem.Id, out var _))
            {
                Debug.LogWarning("Failed to remove existing item during swap");
                return false;
            }

            if (!parentInventory.RemoveItemById(inventoryItem.Id, out var _))
            {
                Debug.LogWarning("Failed to remove dragged item during swap");
                // Пытаемся вернуть existingItem обратно
                destinationInventory.AddExistingItemAt(existingItem, existingPos.x, existingPos.y, existingRotated);
                return false;
            }

            // Добавляем перетаскиваемый предмет в целевой инвентарь
            if (!destinationInventory.AddExistingItemAt(
                inventoryItem,
                gridPosition.x,
                gridPosition.y,
                rotated
            ))
            {
                Debug.LogWarning("Failed to add dragged item to destination during swap");
                // Откатываем изменения
                parentInventory.AddExistingItemAt(inventoryItem, inventoryItem.GridPosition.x, inventoryItem.GridPosition.y, inventoryItem.IsRotated);
                destinationInventory.AddExistingItemAt(existingItem, existingPos.x, existingPos.y, existingRotated);
                return false;
            }

            // Пытаемся добавить существующий предмет в исходный инвентарь
            // Для экипировки - в исходную позицию перетаскиваемого предмета
            // Для обычного инвентаря - ищем свободное место
            bool addedExisting = false;

            if (isEquipmentSlot)
            {
                // Возвращаем в исходную позицию
                addedExisting = parentInventory.AddExistingItemAt(
                    existingItem,
                    inventoryItem.GridPosition.x,
                    inventoryItem.GridPosition.y,
                    inventoryItem.IsRotated
                );
            }
            else
            {
                // Пытаемся добавить в любое свободное место
                addedExisting = parentInventory.AddExistingItem(existingItem);
            }

            if (!addedExisting)
            {
                Debug.LogWarning("Failed to add existing item to source inventory during swap - item may be lost!");
                // В идеале нужно откатить всю операцию, но это сложно
                // Можно попробовать добавить в целевой инвентарь
                if (!destinationInventory.AddExistingItem(existingItem))
                {
                    Debug.LogError("CRITICAL: Item lost during swap operation!");
                }
            }

            return true;
        }
        // ОБЫЧНОЕ ПЕРЕМЕЩЕНИЕ: Если целевой слот пустой
        else
        {
            if (
                destinationInventory.AddExistingItemAt(
                    inventoryItem,
                    gridPosition.x,
                    gridPosition.y,
                    rotated
                )
            )
            {
                if (parentInventory.RemoveItemById(inventoryItem.Id, out var _))
                {
                    return true;
                }
                else
                {
                    Debug.Log("this should never happen");
                }
                }
            }
        }

        return false;
    }

    private void InitializeEquipmentInventories()
    {
        _equipmentInventories.Clear();
        CreateEquipSlot(EquipmentSlotType.Head);
        CreateEquipSlot(EquipmentSlotType.Vest);
        CreateEquipSlot(EquipmentSlotType.Shirt);
        CreateEquipSlot(EquipmentSlotType.Pants);
        CreateEquipSlot(EquipmentSlotType.Socks);
        CreateEquipSlot(EquipmentSlotType.Boots);
        CreateEquipSlot(EquipmentSlotType.MainHand);
        CreateEquipSlot(EquipmentSlotType.OffHand);
    }

    private void CreateEquipSlot(EquipmentSlotType slot)
    {
        var id = GetEquipmentInventoryId(slot);
        var inv = new TetrisInventory(new Vector2Int(1, 1));
        inv.TreatAllItemsAsUnit = true;
        _equipmentInventories[id] = inv;
    }

    public static string GetEquipmentInventoryId(EquipmentSlotType slot)
    {
        return $"equip:{slot}";
    }

    private static EquipmentSlotType ParseSlotFromInventoryId(string id)
    {
        if (string.IsNullOrEmpty(id) || !id.StartsWith("equip:"))
        {
            return EquipmentSlotType.None;
        }

        var name = id.Substring("equip:".Length);
        if (System.Enum.TryParse<EquipmentSlotType>(name, out var slot))
        {
            return slot;
        }
        return EquipmentSlotType.None;
    }

    public bool AddItem(InventoryItemSO item)
    {
        return _inventory.AddItem(item, out var _);
    }

    public void Sort(string inventoryId)
    {
        if (inventoryId == null)
        {
            _inventory.Sort();
        }
        else
        {
            var dynamicItem = GetDynamicItemById(inventoryId);

            if (dynamicItem != null && dynamicItem is IDynamicBackpackInventoryItem backpackItem)
            {
                backpackItem.Inventory.Sort();
            }
        }
    }

    #region Serialize / Deserialize

    [ContextMenu(nameof(Save))]
    public void Save()
    {
        var bytes = _serialization.Serialize(_inventory);
        _saveSystem.SetBytes(kSaveKey, bytes);
    }

    [ContextMenu(nameof(Load))]
    public void Load()
    {
        try
        {
            LoadInternal();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void LoadInternal()
    {
        var bytes = _saveSystem.GetBytes(kSaveKey);
        if (bytes == null)
        {
            return;
        }

        var uninitializedInventory = _serialization.Deserialize(bytes);

        if (uninitializedInventory == null)
        {
            uninitializedInventory = new TetrisInventory();
        }

        _inventory = new TetrisInventory(_gridSize);

        for (int i = 0; i < uninitializedInventory.Items.Count; i++)
        {
            var item = uninitializedInventory.Items[i];

            if (item == null)
            {
                continue;
            }

            // Dima: for binary use "_inventory.AddCopyItemAt"

            _inventory.AddExistingItemAt(
                item,
                item.GridPosition.x,
                item.GridPosition.y,
                item.IsRotated
            );
        }

        if (Application.isPlaying)
        {
            _view.BindAndSync(_inventory);
        }
    }

    private ISaveSystem GetSaveSystem()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return new WebglSaveSystem();
#else
        return FileSaveSystem.Persistent;
#endif
    }

    private ISerialization GetSerialization()
    {
        return new NewtonsoftJsonSerialization(this);
    }
    #endregion
}
