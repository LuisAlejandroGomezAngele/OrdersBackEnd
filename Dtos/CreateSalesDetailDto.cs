namespace MyApi.Dtos
{
    public class CreateSalesDetailDto
    {
        public int Sale_ID { get; set; }
        public string Code { get; set; }
        public decimal Price { get; set; } // Cambia a decimal si la base de datos usa decimal
        public string Barcode { get; set; }
        public double Amount { get; set; } // Cambia de float a double si la base de datos usa float
    }
}
