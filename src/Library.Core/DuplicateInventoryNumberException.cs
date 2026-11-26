namespace Library.Core;

public sealed class DuplicateInventoryNumberException : InvalidOperationException
{
    public DuplicateInventoryNumberException(string inventoryNumber)
        : base($"Inventory number '{inventoryNumber}' already exists.")
    {
        InventoryNumber = inventoryNumber;
    }

    public DuplicateInventoryNumberException(string inventoryNumber, Exception innerException)
        : base($"Inventory number '{inventoryNumber}' already exists.", innerException)
    {
        InventoryNumber = inventoryNumber;
    }

    public string InventoryNumber { get; }
}
