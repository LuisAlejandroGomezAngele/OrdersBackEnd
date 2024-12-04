using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Data.SqlTypes;

[Route("api/Shipments")]
[ApiController]
public class ShipmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ShipmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("getOrdersInProcess")]
    public async Task<IActionResult> GetOrdersInProcess()
    {
        try
        {
            var ordersInProcess = await _context.Sales
                .Join(
                    _context.Warehouses,
                    o => o.WarehouseID,
                    w => w.Id,
                    (o, w) => new
                    {
                        OrderId = o.Id,
                        Order = o.Motion,
                        WarehouseName = w.Name,
                        Status = o.Status,
                        RegistrationDate = o.Registration_date,
                        TiempoSurtido = o.Time_shipments // Incluye la columna Time_shipments
                    }
                )
                .Where(order => order.Status == "En surtido" ||
                                order.Status == "En Pausa" ||
                                order.Status == "Surtido finalizado")
                .Select(order => new
                {
                    order.OrderId,
                    order.Order,
                    order.WarehouseName,
                    order.Status,
                    FormattedDate = order.RegistrationDate.ToString("dddd, dd MMMM yyyy"),
                    TiempoSurtido = order.TiempoSurtido ?? "No registrado" // Manejo de nulos
                })
                .ToListAsync();

            return Ok(ordersInProcess);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Se produjo un error al recuperar pedidos en surtido.", details = ex.Message });
        }
    }


    [HttpGet("{id}/shipmentDetails")]
    public async Task<IActionResult> GetShipmentDetails(int id)
    {
        try
        {
            var shipmentDetails = await _context.Sales
                .Where(sale => sale.Id == id) 
                .Join(
                    _context.Sales_detail, 
                    sale => sale.Id, 
                    detail => detail.Sale_ID, 
                    (sale, detail) => new { sale, detail }
                )
                .Join(
                    _context.Warehouses,
                    combined => combined.sale.WarehouseID, 
                    warehouse => warehouse.Id,
                    (combined, warehouse) => new { combined.sale, combined.detail, warehouse }
                )
                .Join(
                    _context.Products, 
                    combined => combined.detail.Code, 
                    product => product.Code, 
                    (combined, product) => new
                    {
                        Code = product.Code,
                        Description = product.Name,
                        Warehouse = combined.warehouse.Name,
                        Available = _context.Products_inventory
                            .Where(pi => pi.ProductId == product.Id && pi.WarehouseId == combined.warehouse.Id)
                            .Select(pi => pi.Stock)
                            .FirstOrDefault(), 
                        TotalQuantity = combined.detail.Amount, 
                        Scanned = combined.detail.Confirmed_amount, 
                        Pending = combined.detail.Pendin_amount 
                    }
                )
                .ToListAsync();

            if (shipmentDetails == null || !shipmentDetails.Any())
            {
                return Ok(new List<object>());
            }

            return Ok(shipmentDetails);
        }
        catch (Exception ex)
        {
            // Manejo de errores
            return StatusCode(500, new { Message = "Error al recuperar los detalles del envío.", Details = ex.Message });
        }
    }
    [HttpPost("{id}/validateBarcode")]
    public async Task<IActionResult> ValidateBarcode(int id, [FromBody] BarcodeRequest barcodeRequest)
    {
        if (string.IsNullOrEmpty(barcodeRequest.Barcode))
        {
            return BadRequest(new { ok = 1, message = "El código de barras no puede estar vacío." });
        }

        var exists = await _context.Sales_detail
            .AnyAsync(detail => detail.Sale_ID == id && detail.Barcode == barcodeRequest.Barcode);

        if (exists)
        {
            return Ok(new { ok = 0, message = "El código de barras existe en los detalles de la orden." });
        }
        else
        {
            return Ok(new { ok = 1, message = "El código de barras no existe en los detalles de la orden." });
        }
    }

    [HttpPost("{id}/addScannedQuantity")]
    public async Task<IActionResult> AddScannedQuantity(int id, [FromBody] AddScannedQuantityRequest request)
    {
        if (string.IsNullOrEmpty(request.Barcode))
        {
            return Ok(new { ok = 1, message = "El código de barras no puede estar vacío." });
        }

        if (request.Quantity <= 0)
        {
            return Ok(new { ok = 1, message = "La cantidad debe ser mayor a cero." });
        }

        var detail = await _context.Sales_detail
            .FirstOrDefaultAsync(d => d.Sale_ID == id && d.Barcode == request.Barcode);

        if (detail == null)
        {
            return Ok(new { ok = 1, message = "El detalle de la orden con el código de barras proporcionado no existe." });
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Code == detail.Code);

        if (product == null)
        {
            return Ok(new { ok = 1, message = "El producto asociado al detalle no se encontró." });
        }

        var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == id);
        var productInventory = await _context.Products_inventory
            .FirstOrDefaultAsync(pi => pi.ProductId == product.Id && pi.WarehouseId == sale.WarehouseID);

        if (productInventory == null)
        {
            return Ok(new { ok = 1, message = "El inventario del producto no se encontró en el almacén." });
        }

        var pendingAmount = detail.Amount - detail.Confirmed_amount;
        if (request.Quantity > pendingAmount)
        {
            return Ok(new { ok = 1, message = "La cantidad escaneada excede la cantidad pendiente." });
        }

        if (request.Quantity > productInventory.Stock)
        {
            return Ok(new { ok = 1, message = "La cantidad escaneada excede el stock disponible en el inventario." });
        }

        detail.Confirmed_amount += request.Quantity;

        detail.Pendin_amount = pendingAmount - request.Quantity;

        try
        {
            await _context.SaveChangesAsync();
            return Ok(new
            {
                ok = 0,
                message = "Cantidad escaneada agregada exitosamente.",
                confirmedAmount = detail.Confirmed_amount,
                pendingAmount = detail.Pendin_amount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = 1, message = "Ocurrió un error al actualizar la cantidad escaneada.", details = ex.Message });
        }
    }

    [HttpPost("{id}/resetScannedQuantity")]
    public async Task<IActionResult> ResetScannedQuantity(int id, [FromBody] ResetScannedQuantityDto resetDto)
    {
        if (resetDto == null || string.IsNullOrEmpty(resetDto.Code))
        {
            return BadRequest(new { ok = 1, message = "El código no puede estar vacío." });
        }

        var detail = await _context.Sales_detail
            .FirstOrDefaultAsync(d => d.Sale_ID == id && d.Code == resetDto.Code);

        if (detail == null)
        {
            return NotFound(new { ok = 1, message = "El detalle con el código especificado no existe." });
        }

        detail.Confirmed_amount = 0;
        detail.Pendin_amount = detail.Amount; 

        try
        {
            await _context.SaveChangesAsync();
            return Ok(new { ok = 0, message = "La cantidad escaneada fue reseteada exitosamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = 1, message = "Error al resetear las cantidades.", details = ex.Message });
        }
    }

    [HttpPut("{id}/pauseOrResume")]
    public async Task<IActionResult> PauseOrResumeShipment(int id, [FromBody] UpdateStatusDto statusDto)
    {
        var sale = await _context.Sales.FindAsync(id);

        if (sale == null)
        {
            return NotFound(new { Message = $"Sale with ID {id} not found." });
        }

        try
        {
            sale.Status = statusDto.Status;

            var log = new log_sales
            {
                Sale_iD = id,
                New_status = statusDto.Status,
                Modification_date = DateTime.Now
            };
            _context.Log_sales.Add(log);

            await _context.SaveChangesAsync();

            return Ok(new { ok = 0, Message = $"Estado del surtido actualizado a'{statusDto.Status}' exitosamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = 1, Message = "Error al actualizar el estado del surtido.", Details = ex.Message });
        }
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetShipmentStatus(int id)
    {
        try
        {
            var sale = await _context.Sales
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    Status = s.Status 
                })
                .FirstOrDefaultAsync();

            if (sale == null)
            {
                return NotFound(new { Message = $"No se encontró ningún surtido con ID {id}." });
            }

            return Ok(sale);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Se produjo un error al recuperar el estado del surtido..", Details = ex.Message });
        }
    }

    [HttpPost("{id}/finalize")]
    public async Task<IActionResult> FinalizeShipment(int id)
    {
        try
        {
            var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == id);
            if (sale == null)
            {
                return NotFound(new { Message = $"Sale with ID {id} no encontrada." });
            }


            var orderDetails = await _context.Sales_detail
                .Where(detail => detail.Sale_ID == id)
                .ToListAsync();

            if (!orderDetails.Any())
            {
                return NotFound(new { Message = "No se encontraron detalles del pedido para el ID de envío indicado." });
            }

            foreach (var detail in orderDetails)
            {

                if (string.IsNullOrEmpty(detail.Code))
                {
                    return BadRequest(new { Message = "Los detalles del pedido contienen un código nulo o no válido.", Detail = detail });
                }

                var product = await _context.Products.FirstOrDefaultAsync(p => p.Code == detail.Code);
                if (product == null)
                {
                    return NotFound(new { Message = $"Producto con código {detail.Code} no encontrado." });
                }


                var productInventory = await _context.Products_inventory
                    .FirstOrDefaultAsync(pi => pi.ProductId == product.Id && pi.WarehouseId == sale.WarehouseID);

                if (productInventory == null)
                {
                    return NotFound(new { Message = "Inventario no encontrado para el producto." });
                }


                if (productInventory.Stock < detail.Confirmed_amount)
                {
                    return BadRequest(new
                    {
                        Message = "Stock insuficiente para finalizar el surtido.",
                        ProductId = product.Id,
                        Stock = productInventory.Stock,
                        ConfirmedAmount = detail.Confirmed_amount
                    });
                }

                productInventory.Stock -= (int)detail.Confirmed_amount;
                detail.Pendin_amount = 0;

            }

            var logs = await _context.Log_sales
                .Where(log => log.Sale_iD == id && (log.New_status == "En surtido" || log.New_status == "En Pausa"))
                .OrderBy(log => log.Modification_date)
                .ToListAsync();

            TimeSpan totalTime = TimeSpan.Zero;
            DateTime? startTime = null;

            foreach (var log in logs)
            {

                if (log.New_status == "En surtido")
                {
                    startTime = log.Modification_date;
                }
                else if (log.New_status == "En Pausa" && startTime.HasValue)
                {
                    totalTime += log.Modification_date - startTime.Value;
                    startTime = null;
                }
            }

            if (startTime.HasValue)
            {
                totalTime += DateTime.Now - startTime.Value;
            }

            sale.Status = "Surtido finalizado";
            sale.Time_shipments = totalTime.ToString(@"hh\:mm\:ss");


            _context.Log_sales.Add(new log_sales
            {
                Sale_iD = id,
                New_status = "Surtido finalizado",
                Modification_date = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Surtido finalizado con éxito.", TotalTime = sale.Time_shipments });
        }
        catch (SqlNullValueException sqlEx)
        {
            return StatusCode(500, new { Message = "Se encontró un valor nulo en la base de datos.", Details = sqlEx.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Se produjo un error durante la finalización del surtido.", Details = ex.Message });
        }
    }

}
