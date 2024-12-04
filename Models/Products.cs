using System.ComponentModel.DataAnnotations.Schema;

public class Products
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? Spec { get; set; }

    public string? Barcode { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; }
}
