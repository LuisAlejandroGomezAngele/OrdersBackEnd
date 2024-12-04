using System.ComponentModel.DataAnnotations.Schema;

public class products_inventory
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int WarehouseId { get; set; }

    public int Stock { get; set; }

}
