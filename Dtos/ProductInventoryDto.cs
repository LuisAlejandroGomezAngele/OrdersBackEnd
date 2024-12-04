namespace MyApi.Dtos
{
    public class ProductInventoryDto
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public string Barcode { get; set; }

    }
}
