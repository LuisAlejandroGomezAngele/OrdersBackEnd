public class sales_detail
{
    public int Id { get; set; }
    public int Sale_ID { get; set; }
    public string? Code { get; set; }
    public string? Barcode { get; set; }
    public double Amount { get; set; } 
    public decimal Price { get; set; } 
    public double Pendin_amount { get; set; } 
    public double Confirmed_amount { get; set; }
    public double Reserved_amount { get; set; } 

}
