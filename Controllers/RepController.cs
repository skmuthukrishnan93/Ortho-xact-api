using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Ortho_xact_api.DTO;
using Ortho_xact_api.Models;
using Ortho_xact_api.Services;
using Ortho_xact_api.SysModels;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using System.Text;

namespace Ortho_xact_api.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("[controller]")]
    public class RepController : ControllerBase
    {



        private readonly IConfiguration _configuration;

        private readonly OrthoxactContext _context;
        private readonly SysproContext _sysContext;

        public RepController(OrthoxactContext context, IConfiguration configuration, SysproContext sysContext)
        {
            _context = context;
            _configuration = configuration;
            _sysContext = sysContext;
        }
        [HttpPost("clerksave")]
        public async Task<IActionResult> SaveClerkOrders([FromBody] DeliveryOrderDetailPayload payload)
        {
            var items = payload.Data;
            if (items == null || !items.Any())
                return BadRequest("No data received.");
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            // Group by SalesOrder
            var groupedBySalesOrder = items
                .GroupBy(dto => dto.SalesOrder);

            var entities = new List<DeliveryOrderDetail>();
            int count = _context.DeliveryOrderDetails
   .Select(dd => dd.ClerkVerNumber)
   .Distinct()
   .Count();
            string finalNumber = "Clerk-" + count.ToString("D7");

            foreach (var group in groupedBySalesOrder)
            {
                var group1 = group.Where(c => c.MbomFlag != "P");
                bool allHaveQty = group1.All(x => x.RetQty.HasValue);
                bool anyHaveQty = group1.Any(x => x.RetQty.HasValue);

                string status = allHaveQty ? "Completed&ReadyForValidation" :
                                anyHaveQty ? "StoresInProgress" : null;

                foreach (var dto in group)
                {
                    var existing = await _context.DeliveryOrderDetails
        .FirstOrDefaultAsync(x => x.SalesOrder == dto.SalesOrder && x.Line == dto.SalesOrderLine);

                    if (existing != null)
                    {
                        // Update existing
                       existing.RetQty= dto.RetQty;
                        existing.Status = status;
                        existing.Usage = existing.MshipQty - dto.RetQty ?? 0;
                        existing.Variance = existing.RepUsageQty - existing.Usage ?? 0;
                        existing.ClerkDate =DateTime.Now;
                        existing.ClerkName = username;
                        existing.ClerkVerNumber = finalNumber;
                    }
                    
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Saved successfully", count = entities.Count });

        }
        [HttpPost("clerkvalidate")]
        public async Task<IActionResult> ValidateOrders([FromBody] DeliveryOrderDetailPayload payload)
        {
            var items = payload.Data;
            if (items == null || !items.Any())
                return BadRequest("No data received.");
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            // Group by SalesOrder
            var groupedBySalesOrder = items
                .GroupBy(dto => dto.SalesOrder);

            var entities = new List<DeliveryOrderDetail>();
            int count = _context.DeliveryOrderDetails
   .Select(dd => dd.ClerkVerNumber)
   .Distinct()
   .Count();
            string finalNumber = "Clerk-" + count.ToString("D7");

            foreach (var group in groupedBySalesOrder)
            {
                var group1 = group.Where(c => c.MbomFlag != "P");
                bool allHaveQty = group1.All(x => x.RepUsageQty.HasValue);
                bool anyHaveQty = group1.Any(x => x.RepUsageQty.HasValue);
                if (!allHaveQty)
                {
                    return BadRequest("Validation Failed");
                }

                string status = "ReadyToPostSyspro";

                foreach (var dto in group)
                {
                    var existing = await _context.DeliveryOrderDetails
        .FirstOrDefaultAsync(x => x.SalesOrder == dto.SalesOrder && x.Line == dto.SalesOrderLine);

                    if (existing != null)
                    {
                        
                        existing.Status = status;
                        
                    }

                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Saved successfully", count = entities.Count });

        }

        [HttpPost("posttosyspro")]
        public async Task<IActionResult> PostToSyspro([FromBody] DeliveryOrderDetailPayload payload)
        {
            var items = payload.Data;
            if (items == null || !items.Any())
                return BadRequest("No data received.");
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            // Group by SalesOrder
            var groupedBySalesOrder = items
                .GroupBy(dto => dto.SalesOrder);


            var existingMaster = new SorMaster();
            var existingOrderDetails = new List<SorDetail>();

            foreach (var group in groupedBySalesOrder)
            {
                var group1 = group.Where(c => c.MbomFlag != "P");
                bool allHaveQty = group1.All(x => x.RepUsageQty.HasValue);
                bool anyHaveQty = group1.Any(x => x.RepUsageQty.HasValue);
                if (!allHaveQty)
                {
                    return BadRequest("Validation Failed");
                }
                var salesorder = group.FirstOrDefault()?.SalesOrder;
                 existingMaster = await _sysContext.SorMasters.Where(c => c.SalesOrder == salesorder ).FirstOrDefaultAsync();
                 existingOrderDetails = await _sysContext.SorDetails.Where(c => c.SalesOrder == salesorder && c.MbomFlag != "P").ToListAsync();
                string status = "PostedToSyspro";
                existingMaster.OrderStatus = "1";
                foreach (var dto in group)
                {
                    var dtls = existingOrderDetails.FirstOrDefault(x => x.SalesOrder == dto.SalesOrder && x.SalesOrderLine == dto.SalesOrderLine && x.MbomFlag !="P");
                    if (dtls != null)
                    {
                        dtls.MorderQty= (decimal)dto.Usage;
                        dtls.MshipQty = (decimal)dto.Usage;
                      
                       
                    }
                    var existing = await _context.DeliveryOrderDetails
        .FirstOrDefaultAsync(x => x.SalesOrder == dto.SalesOrder && x.Line == dto.SalesOrderLine);

                    if (existing != null)
                    {

                        existing.Status = status;
                        existing.PostedBy = username;
                        existing.PostedDate = DateTime.Now;
                    }

                }
            }
            await _sysContext.SaveChangesAsync();
            await _context.SaveChangesAsync();
            var syspro = new SysproWebService();
            var responseXml = await syspro.LoginAsync("CONS29", "", "OXZ");
            var sessionId = responseXml.Body.LogonResult;
            //return Ok(new { response = responseXml.Body.LogonResult });
            // var sessionId = await LoginAsync("EDU", "ADMIN", "1234");


            string xmlIn = GenerateSortoiXml(existingMaster, existingOrderDetails);
            string parameters = GenerateSortoiParametersXml();
            var response = await syspro.Transaction(sessionId, "SORTSU", parameters, xmlIn);
       
            return Ok(new { message = "Saved successfully", count = 0 });

        }

        [HttpPost("testposttosyspro")]
        public async Task<IActionResult> TestPostToSyspro([FromBody] string payload)
        {
            


            var existingMaster = new SorMaster();
            var existingOrderDetails = new List<SorDetail>();


            var salesorder = "000000000030516";
                existingMaster = await _sysContext.SorMasters.Where(c => c.SalesOrder == salesorder).FirstOrDefaultAsync();
                existingOrderDetails = await _sysContext.SorDetails.Where(c => c.SalesOrder == salesorder).ToListAsync();
                
           
            var syspro = new SysproWebService();
            var responseXml = await syspro.LoginAsync("CONS29", "", "OXZ");
            var sessionId = responseXml.Body.LogonResult;
            //return Ok(new { response = responseXml.Body.LogonResult });
            // var sessionId = await LoginAsync("EDU", "ADMIN", "1234");


            string xmlIn = GenerateSortoiXml2(existingMaster, existingOrderDetails);
            string parameters = GenerateSortoiParametersXml1();
            var response = await syspro.Transaction(sessionId, "SORTDM", parameters, xmlIn);

            return Ok(new { message = "Saved successfully", count = 0 });

        }
        private string GenerateSortoiParametersXml1(bool validateOnly = false, bool ignoreWarnings = false)
        {
            return $@"<?xml version=""1.0"" encoding=""Windows-1252""?><PostDispMaint xmlns:xsd=""http://www.w3.org/2001/XMLSchema-instance"" xsd:noNamespaceSchemaLocation=""SORTDM.XSD"">
<Parameters>
  <ValidateOnly>{(validateOnly ? "Y" : "N")}</ValidateOnly>
  <IgnoreWarnings>{(ignoreWarnings ? "Y" : "N")}</IgnoreWarnings>
</Parameters></PostDispMaint>".Trim();
        }
        private string GenerateSortoiParametersXml(bool validateOnly = false, bool ignoreWarnings = false)
        {
            return $@"<?xml version=""1.0"" encoding=""Windows-1252""?><PostChangeSalesOrderKitComp xmlns:xsd=""http://www.w3.org/2001/XMLSchema-instance"" xsd:noNamespaceSchemaLocation=""SORTSU.XSD"">
<Parameters>
  <ValidateOnly>{(validateOnly ? "Y" : "N")}</ValidateOnly>
  <IgnoreWarnings>{(ignoreWarnings ? "Y" : "N")}</IgnoreWarnings>
</Parameters></PostChangeSalesOrderKitComp>".Trim();
        }
        private string GenerateSortoiXml(SorMaster master, List<SorDetail> lines)
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"<?xml version=""1.0"" encoding=""Windows-1252""?>");
            sb.AppendLine(@"<SalesOrders xmlns:xsd=""http://www.w3.org/2001/XMLSchema-instance"" xsd:noNamespaceSchemaLocation=""SORTOIDOC.XSD"">");

            // Transmission Header
            sb.AppendLine("  <TransmissionHeader>");
            sb.AppendLine("    <TransmissionReference>00000000000001</TransmissionReference>");
            sb.AppendLine("    <SenderCode />");
            sb.AppendLine("    <ReceiverCode>HO</ReceiverCode>");
            sb.AppendLine($"    <DatePrepared>{DateTime.Now:yyyy-MM-dd}</DatePrepared>");
            sb.AppendLine($"    <TimePrepared>{DateTime.Now:HH:mm}</TimePrepared>");
            sb.AppendLine("  </TransmissionHeader>");

            // Order section
            sb.AppendLine("  <Orders>");
            sb.AppendLine("    <OrderHeader>");
            sb.AppendLine($"      <CustomerPoNumber>{SecurityElement.Escape(master.CustomerPoNumber)}</CustomerPoNumber>");
            sb.AppendLine("      <OrderActionType>C</OrderActionType>"); // Change existing order
            sb.AppendLine($"      <Customer>{master.Customer}</Customer>");
            sb.AppendLine($"      <OrderDate>{master.OrderDate:yyyy-MM-dd}</OrderDate>");
            sb.AppendLine($"      <Warehouse>{master.Warehouse}</Warehouse>");
            sb.AppendLine($"      <OrderStatus>1</OrderStatus>");
            sb.AppendLine($"      <SalesOrder>{master.SalesOrder}</SalesOrder>");
            sb.AppendLine("    </OrderHeader>");

            sb.AppendLine("    <OrderDetails>");
            foreach (var line in lines)
            {
                sb.AppendLine("      <StockLine>");
                sb.AppendLine($"        <LineActionType>C</LineActionType>");
                sb.AppendLine($"        <CustomerPoLine>{line.SalesOrderLine}</CustomerPoLine>");
                sb.AppendLine($"        <OrderQty>{line.MorderQty}</OrderQty>");
              //  sb.AppendLine($"        <ShipQty>0</ShipQty>");
                sb.AppendLine("      </StockLine>");
            }
            sb.AppendLine("    </OrderDetails>");
            sb.AppendLine("  </Orders>");
            sb.AppendLine("</SalesOrders>");

            return sb.ToString();
        }

        private string GenerateSortoiXml1(SorMaster master, List<SorDetail> lines)
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"<?xml version=""1.0"" encoding=""Windows-1252""?>");
            sb.AppendLine(@"<PostChangeSalesOrderKitComp xmlns:xsd=""http://www.w3.org/2001/XMLSchema-instance"" xsd:noNamespaceSchemaLocation=""SORTSUDOC.XSD"">");

            

            
            foreach (var line in lines)
            {
                sb.AppendLine("    <Item>");
                sb.AppendLine($"      <SalesOrder> {master.SalesOrder}</SalesOrder>");
                sb.AppendLine($"        <Customer>{master.Customer}</Customer>");
                sb.AppendLine($"        <SalesOrderLine>{line.SalesOrderLine}</SalesOrderLine>");
                sb.AppendLine($"        <OrderQty>{line.MorderQty}</OrderQty>");
              //  sb.AppendLine($"        <ShipQty>0</ShipQty>");
                sb.AppendLine($"        <StockCode>{line.MstockCode}</StockCode>");
     
                sb.AppendLine("    </Item>");
            }

            sb.AppendLine("</PostChangeSalesOrderKitComp>");

            return sb.ToString();
        }
        private string GenerateSortoiXml2(SorMaster master, List<SorDetail> lines)
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"<?xml version=""1.0"" encoding=""Windows-1252""?>");
            sb.AppendLine(@"<PostDispMaint xmlns:xsd=""http://www.w3.org/2001/XMLSchema-instance"" xsd:noNamespaceSchemaLocation=""SORTDMDOC.XSD"">");




            foreach (var line in lines)
            {
                sb.AppendLine("    <Item>");
                sb.AppendLine("    <MerchandiseLine>");
                sb.AppendLine($"      <DispatchNote>{master.SalesOrder}</DispatchNote>");
                sb.AppendLine($"        <DispatchLine>{line.SalesOrderLine}</DispatchLine>");
                sb.AppendLine($"        <DispatchQty>0</DispatchQty>");
                sb.AppendLine("    </MerchandiseLine>");
                sb.AppendLine("    </Item>");
            }

            sb.AppendLine("</PostDispMaint>");

            return sb.ToString();
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveDeliveryOrders([FromBody] DeliveryOrderDetailPayload payload)
        {
            var items = payload.Data;
            if (items == null || !items.Any())
                return BadRequest("No data received.");
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            
            // Group by SalesOrder
            var groupedBySalesOrder = items
                .GroupBy(dto => dto.SalesOrder);

            var entities = new List<DeliveryOrderDetail>();
            int count = _context.DeliveryOrderDetails
    .Select(dd => dd.RepVerNumber)
    .Distinct()
    .Count();
            string finalNumber = "Rep-" + count.ToString("D7");
            foreach (var group in groupedBySalesOrder)
            {
                var group1 = group.Where(c => c.MbomFlag != "P");
                bool allHaveQty = group1.All(x => x.RepUsageQty.HasValue);
                bool anyHaveQty = group1.Any(x => x.RepUsageQty.HasValue);

                string status = allHaveQty ? "RepCompleted" :
                                anyHaveQty ? "Inprogress" : null;

                foreach (var dto in group)
                {
                    var existing = await _context.DeliveryOrderDetails
        .FirstOrDefaultAsync(x => x.SalesOrder == dto.SalesOrder && x.Line == dto.SalesOrderLine);

                    if (existing != null)
                    {
                        // Update existing
                        existing.Customer = dto.Customer;
                        existing.CustomerName = dto.CustomerName;
                        existing.Status =status;
                        existing.Sysprostatus = dto.Sysprostatus;
                        existing.Set = dto.SetsCode;
                        existing.Mwarehouse = dto.Mwarehouse;
                        existing.MstockCode = dto.MstockCode;
                        existing.MstockDes = dto.MstockDes;
                        existing.MorderQty = dto.MorderQty;
                        existing.MshipQty = dto.MshipQty;
                        existing.RepUsageQty = dto.RepUsageQty;
                        existing.RepEntertedDate = DateTime.Now;
                        existing.RepName = username;
                    }
                    else
                    {
                        
                            // Insert new
                            var newEntity = new DeliveryOrderDetail
                            {
                                SalesOrder = dto.SalesOrder,
                                Line = dto.SalesOrderLine,
                                Customer = dto.Customer,
                                CustomerName = dto.CustomerName,
                                Status = status,
                                Sysprostatus = dto.Sysprostatus,
                                Set = dto.SetsCode,
                                Mwarehouse = dto.Mwarehouse,
                                MstockCode = dto.MstockCode,
                                MstockDes = dto.MstockDes,
                                MorderQty = dto.MorderQty,
                                MshipQty = dto.MshipQty,
                                RepUsageQty = dto.RepUsageQty,
                                RepEntertedDate = DateTime.Now,
                                RepName = username,
                                RepVerNumber= finalNumber,
                            };

                            _context.DeliveryOrderDetails.Add(newEntity);
                       
                    }
                }
                await _context.SaveChangesAsync();
               
            }

            

            return Ok(new { message = "Saved successfully", count = entities.Count });

        }

        [HttpPost("updatestatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] DeliveryOrderDetailPayload payload)
        {
            var items = payload.Data;
            if (items == null || !items.Any())
                return BadRequest("No data received.");
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            // Group by SalesOrder
            var groupedBySalesOrder = items
                .GroupBy(dto => dto.SalesOrder);

            
            foreach (var group in groupedBySalesOrder)
            {
                

                string status = "StoresInProgress";

                foreach (var dto in group)
                {
                    var existing = await _context.DeliveryOrderDetails
        .FirstOrDefaultAsync(x => x.SalesOrder == dto.SalesOrder && x.Line == dto.SalesOrderLine);

                    if (existing != null)
                    {
                        // Update existing
                       
                        existing.Status = status;
                       
                    }
                   
                }
                await _context.SaveChangesAsync();

            }



            return Ok(new { message = "Saved successfully", count = 0 });

        }
        [HttpPost("revieworderdetails")]
        public async Task<IActionResult> GetAdminSalesOrders([FromBody] SalesOrderRequest request)
        {



            var order = await _sysContext.VwFetchSordetails.Where(o => o.OrderStatus == "4")
                .ToListAsync();
            if (!String.IsNullOrEmpty(request?.SalesOrderNumber))
                order = order
                    .Where(o => o.SalesOrder.Contains(request.SalesOrderNumber))
                    .ToList();

            if (order?.Count == 0)
                return NotFound("Sales order not found.");

            return Ok(order);
        }


        [HttpGet("GetDeliveryNotes")]
        public async Task<IActionResult> GetDeliveryNotes()
        {



            var groupedOrders = await _sysContext.VwFetchSordetails
    .Where(o => o.OrderStatus == "4")
    .GroupBy(o => new { o.SalesOrder, o.Customer, o.Status,o.Salesperson })
    .Select(g => new
    {
        SalesOrder = g.Key.SalesOrder.TrimStart('0'),
        Customer = g.Key.Customer,
        Status = g.Key.Status,
        SalesPerson = g.Key.Salesperson,
    })
    .ToListAsync();

            var roles = User.FindFirst(ClaimTypes.Role)?.Value;
            var salesPerson = User.FindFirst(ClaimTypes.GivenName)?.Value;
            if (roles =="rep")
            {
                groupedOrders = groupedOrders.Where(o => o.SalesPerson==salesPerson &&  o.Status == null || o.Status == "Inprogress").ToList();
                
            }
            if (roles == "repclerk")
            {
                groupedOrders = groupedOrders.Where(o => o.Status == "RepCompleted" || o.Status == "StoresInProgress" || o.Status == "Completed&ReadyForValidation" || o.Status == "ReadyToPostSyspro").ToList();

            }
            if (groupedOrders?.Count == 0)
                return NotFound("Sales order not found.");

            return Ok(groupedOrders);
        }
        [HttpGet("GetSalesPerson")]
        public async Task<IActionResult> GetSalesPerson()
        {



            var groupedOrders = await _sysContext.SalSalespeople
    .ToListAsync();

            

            return Ok(groupedOrders);
        }
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {



            var groupedOrders = await _context.Users.OrderBy(x => x.Firstname)
    .ToListAsync();



            return Ok(groupedOrders);
        }

        [HttpPost("clerkorderdetails")]
        public async Task<IActionResult> GetRepClerkSalesOrders([FromBody] SalesOrderRequest request)
        {



            var order = await _sysContext.VwFetchSordetails.Where(o => o.OrderStatus == "4" && o.Status == "RepCompleted" || o.Status == "StoresInProgress" || o.Status == "Completed&ReadyForValidation" || o.Status == "ReadyToPostSyspro")
                .ToListAsync();
            if (!String.IsNullOrEmpty(request?.SalesOrderNumber))
                order = order
                    .Where(o => o.SalesOrder.Contains(request.SalesOrderNumber))
                    .ToList();

            if (order?.Count == 0)
                return NotFound("Sales order not found.");

            return Ok(order);
        }
        [HttpPost("saleorderdetails")]
        public async Task<IActionResult> GetRepSalesOrders([FromBody] SalesOrderRequest request)
        {


            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var order = await _sysContext.VwFetchSordetails.Where(o => o.OrderStatus == "4" && (o.Status == null || o.Status== "Inprogress"))
                .ToListAsync();
            if(!String.IsNullOrEmpty(request?.SalesOrderNumber ))
            order = order
                .Where(o => o.SalesOrder .Contains( request.SalesOrderNumber))
                .ToList();
            var roles = User.FindFirst(ClaimTypes.Role)?.Value;
            var salesPerson = User.FindFirst(ClaimTypes.GivenName)?.Value;
            if (roles == "rep")
            {
                order = order.Where(o => o.Salesperson == salesPerson && o.Status == null || o.Status == "Inprogress").ToList();

            }
            if (roles == "repclerk")
            {
                order = order.Where(o => o.Status == "RepCompleted" || o.Status == "StoresInProgress" || o.Status == "Completed&ReadyForValidation" || o.Status == "ReadyToPostSyspro").ToList();

            }
            if (order?.Count == 0)
                return NotFound("Sales order not found.");

            return Ok(order);
        }
        [HttpPost("GenerateReport")]
        public async Task<IActionResult> GenerateReport([FromBody] SalesOrderRequest request)
        {
            var roles = User.FindFirst(ClaimTypes.Role)?.Value;
            var doctype = 1;
            if(roles != null && roles=="repclerk")
            {
                doctype = 2;
            }
            if (roles != null && roles == "admin")
                doctype = 3;
            HandleExe.RunMyExe(request.SalesOrderNumber,doctype);
            

            return Ok();
        }
        [HttpPost("ViewReport")]
        public IActionResult GetPDF([FromBody] SalesOrderRequest request)
        {
            var roles = User.FindFirst(ClaimTypes.Role)?.Value;
            var doctype = 1;
            if (roles != null && roles == "repclerk")
            {
                doctype = 2;
            }
            if (roles != null && roles == "admin")
                doctype = 3;

            if (long.TryParse(request.SalesOrderNumber, out long numericOrder))
            {
                // Format to 15 digits with leading zeros
                request.SalesOrderNumber = numericOrder.ToString("D15");
            }
            // Example: Retrieve varbinary data from the database
            var pdfData = _context.DocumentDetails
                          .Where(d => d.DocNumber == request.SalesOrderNumber && d.DocType==doctype)
                          .Select(d => d.Document) // PdfFile is varbinary(max)
                          .FirstOrDefault();

            if (pdfData == null || pdfData.Length == 0)
            {
                return NotFound("PDF not found.");
            }

            return File(pdfData, "application/pdf", "document.pdf");
        }


    }
}
