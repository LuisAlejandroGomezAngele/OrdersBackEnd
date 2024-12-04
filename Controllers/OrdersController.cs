using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using MyApi.Dtos;



[Route("api/Orders")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] sales sale)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Establece fechas automáticamente
        sale.Registration_date = DateTime.Now;
        sale.Lastchange = DateTime.Now;

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        // Retorna el registro creado en un arreglo
        return Ok(new[] { sale });
    }


    [HttpPut]
    public async Task<IActionResult> UpdateSales([FromBody] UpdateSalesDto updateSalesDto)
    {
        // Buscar el registro en la base de datos
        var sale = await _context.Sales.FindAsync(updateSalesDto.Id);
        if (sale == null)
        {
            return NotFound(new { Message = $"Venta con el ID {updateSalesDto.Id} no existe." });
        }

        // Actualizar los valores
        sale.WarehouseID = updateSalesDto.WarehouseID;
        sale.Lastchange = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Se produjo un error al actualizar la venta.", Details = ex.Message });
        }

        // Retornar la confirmación
        return Ok(new { Message = "Venta actualizada exitosamente.", Sale = sale });
    }

    [HttpGet("{id}/inventory")]
    public async Task<IActionResult> GetProductsByWarehouse(int id)
    {
        var results = await (from p in _context.Products
                             join pi in _context.Products_inventory on p.Id equals pi.ProductId
                             join s in _context.Sales on pi.WarehouseId equals s.WarehouseID
                             where s.Id == id
                             select new ProductInventoryDto
                             {
                                 Name = p.Name,
                                 Code = p.Code,
                                 Stock = pi.Stock,
                                 Price = p.Price,
                                 Barcode = p.Barcode
                             }).ToListAsync();

        if (!results.Any())
        {
            return NotFound(new { Message = $"No se encontraron productos a la venta con ID{id}." });
        }

        return Ok(results);
    }

    [HttpPost("createSalesDetail")]
    public async Task<IActionResult> CreateOrUpdateSalesDetail([FromBody] CreateSalesDetailDto salesDetailDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Message = "Invalid data", Errors = ModelState });
        }

        try
        {
            var existingDetail = await _context.Sales_detail
                .FirstOrDefaultAsync(detail => detail.Code == salesDetailDto.Code && detail.Sale_ID == salesDetailDto.Sale_ID);

            if (existingDetail != null)
            {
                existingDetail.Amount = salesDetailDto.Amount;
                _context.Sales_detail.Update(existingDetail);
            }
            else
            {
                var newSalesDetail = new sales_detail
                {
                    Sale_ID = salesDetailDto.Sale_ID,
                    Code = salesDetailDto.Code,
                    Barcode = salesDetailDto.Barcode,
                    Amount = salesDetailDto.Amount,
                    Price = salesDetailDto.Price
                };

                _context.Sales_detail.Add(newSalesDetail);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Se produjo un error al crear o actualizar el detalle de ventas.", Details = ex.Message });
        }

        return Ok(new { Message = "Articulo actualizado correctamente" });
    }


    [HttpGet("orderDetail/{sale_id}")]
    public async Task<IActionResult> GetSalesDetailBySaleId(int sale_id)
    {
        var salesDetailsWithProducts = await _context.Sales_detail
            .Where(detail => detail.Sale_ID == sale_id)
            .Join(
                _context.Products,
                detail => detail.Code, 
                product => product.Code, 
                (detail, product) => new
                {
                    ID = detail.Id,
                    detail.Sale_ID,
                    detail.Code,
                    ProductName = product.Name,
                    detail.Amount,
                    detail.Price,
                    detail.Barcode
                }
            )
            .ToListAsync();

        return Ok(salesDetailsWithProducts);
    }




    [HttpGet]
    [Route("getOrdersWithWarehouse")]
    public async Task<IActionResult> GetOrdersWithWarehouse()
    {
        try
        {
            var orders = await _context.Sales
                .Join(
                    _context.Warehouses, 
                    o => o.WarehouseID, 
                    w => w.Id, 
                    (o, w) => new OrderDto
                    {
                        OrderId = o.Id,
                        Order = o.Motion,
                        WarehouseName = w.Name,
                        Status = o.Status,
                        FormattedDate = o.Registration_date.ToString("dddd, dd MMMM yyyy"),
                        Total = o.Total ?? 0.0
                    }
                )
                .ToListAsync();

            return Ok(orders);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Se produjo un error al recuperar los pedidos.", details = ex.Message });
        }
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSaleById(int id)
    {
        try
        {
            var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
            {
                return NotFound(new { message = "Venta no encontrada" });
            }

            var total = await _context.Sales_detail
                .Where(detail => detail.Sale_ID == id)
                .SumAsync(detail => detail.Amount * (double)detail.Price); 

            sale.Total = total;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = sale.Id,
                motion = sale.Motion,
                status = sale.Status,
                warehouseId = sale.WarehouseID,
                registrationDate = sale.Registration_date.ToString("D"), 
                lastChange = sale.Lastchange.ToString("D"), 
                total
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener la venta", details = ex.Message });
        }
    }


    [HttpDelete("deleteOrderDetail/{id}")]
    public async Task<IActionResult> DeleteOrderDetail(int id)
    {
        // Busca el detalle de la orden por ID
        var orderDetail = await _context.Sales_detail.FindAsync(id);

        if (orderDetail == null)
        {
            return NotFound(new { Message = $"No se encontró el detalle de la orden con ID {id}." });
        }

        try
        {
            _context.Sales_detail.Remove(orderDetail);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Detalle de la orden eliminado correctamente.", Deleted = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Ocurrió un error al intentar eliminar el detalle de la orden.", Details = ex.Message, Deleted = false });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateSalesStatus(int id, [FromBody] UpdateStatusDto statusDto)
    {
        // Busca la venta por ID
        var sale = await _context.Sales.FindAsync(id);

        if (sale == null)
        {
            return NotFound(new { Message = $"Sale with ID {id} not found." });
        }
        sale.Status = statusDto.Status;

        try
        {
            if (statusDto.Status == "En surtido")
            {
                var salesDetails = _context.Sales_detail.Where(detail => detail.Sale_ID == id);

                foreach (var detail in salesDetails)
                {
                    detail.Pendin_amount = detail.Amount;
                    detail.Reserved_amount = detail.Amount; 
                }
            }

            var log = new log_sales
            {
                Sale_iD = id,
                New_status = statusDto.Status,
                Modification_date = DateTime.Now
            };
            _context.Log_sales.Add(log);

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Estatus actualizado correctamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error al actualizar el estado o los detalles.", Details = ex.Message });
        }
    }




}


