using System.ComponentModel.DataAnnotations.Schema;

public class log_sales
{
    public int ID { get; set; } // Identity column

    public int? Sale_iD { get; set; }

    public string? New_status { get; set; }

    public DateTime Modification_date { get; set; }

}

