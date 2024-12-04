using System.ComponentModel.DataAnnotations.Schema;

public class sales
{
    public int Id { get; set; } // Identity column

    public string Motion { get; set; }

    public string Status { get; set; }

    public int WarehouseID { get; set; }

    public DateTime Registration_date { get; set; }

    public DateTime Lastchange { get; set; }

    public string? Time_shipments { get; set; }

    public double? Total { get; set; }


}

