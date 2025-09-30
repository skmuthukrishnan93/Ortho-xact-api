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
                status = "Completed&ReadyForValidation";

                foreach (var dto in group)
                {
                    var existing = await _context.DeliveryOrderDetails
        .FirstOrDefaultAsync(x => x.SalesOrder == dto.SalesOrder && x.Line == dto.SalesOrderLine);

                    if (existing != null)
                    {
                        if(dto.RetQty==null)
                        {
                            dto.RetQty = dto.MshipQty;
                        }
                        if(existing.RepUsageQty==null)
                        {
                            existing.RepUsageQty = 0;
                        }
                        // Update existing
                       existing.RetQty= dto.RetQty;
                        existing.Status = status;
                        existing.Usage = existing.MshipQty - dto.RetQty;
                        existing.Variance = existing.RepUsageQty - existing.Usage;
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
               // bool allHaveQty = group1.All(x => x.RepUsageQty.HasValue);
                bool anyHaveQty = group1.Any(x => x.Variance!=0);
                //if (!allHaveQty)
                //{
                //    return BadRequest("Validation Failed");
                //}

                string status = "ReadyToPostSyspro";
                if(anyHaveQty)
                {                     status = "Send Email To Customer Service";
                }

                foreach (var dto in group)
                {
                    var existing = await _context.DeliveryOrderDetails
        .FirstOrDefaultAsync(x => x.SalesOrder == dto.SalesOrder && x.Line == dto.SalesOrderLine);

                    if (existing != null)
                    {
                        if(existing.RepUsageQty==null )
                        {
                            existing.RepUsageQty = 0;
                        }
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
            var parentOrderDetails = new List<SorDetail>();
            var sorDetailBin = new List<SorDetailBin>();

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
                parentOrderDetails = await _sysContext.SorDetails.Where(c => c.SalesOrder == salesorder && c.MbomFlag == "P").ToListAsync();
                sorDetailBin = await _sysContext.SorDetailBins.Where(c => c.SalesOrder == salesorder).ToListAsync();
                string status = "PostedToSyspro";
                existingMaster.OrderStatus = "1";
                foreach (var dto in group)
                {
                    var dtls = existingOrderDetails.FirstOrDefault(x => x.SalesOrder == dto.SalesOrder && x.SalesOrderLine == dto.SalesOrderLine && x.MbomFlag !="P");
                   var  dtlsBin = sorDetailBin.FirstOrDefault(x => x.SalesOrderLine == dto.SalesOrderLine);
                    if (dtls != null)
                    {
                        dtls.MorderQty= (decimal)dto.Usage;
                        dtls.MshipQty = (decimal)dto.Usage;
                        dtls.MstockQtyToShp = (decimal)dto.Usage;
                        dtls.MqtyPer= (decimal)dto.Usage;
                        dtls.MstockUnitMass= (decimal)dto.Usage;
                        if (dto.Usage==0)
                        {
                            dtls.NsrvMinQuantity = dtls.MorderQty;
                            dtls.SalesOrderDetStat = "C";

                        }
                       
                    }
                    var existing = await _context.DeliveryOrderDetails
        .FirstOrDefaultAsync(x => x.SalesOrder == dto.SalesOrder && x.Line == dto.SalesOrderLine);

                    if (existing != null)
                    {

                        existing.Status = status;
                        existing.PostedBy = username;
                        existing.PostedDate = DateTime.Now;
                    }
                    if (dtlsBin != null)
                    {
                        dtlsBin.StockQtyToShip = (decimal)dto.Usage;
                        dtlsBin.QtyReserved= (decimal)dto.Usage;
                        dtlsBin.Bin = parentOrderDetails.FirstOrDefault()?.MstockCode ?? string.Empty;
                    }

                }
            }
            await _sysContext.SaveChangesAsync();
            await _context.SaveChangesAsync();
            var syspro = new SysproWebService();
            var responseXml = await syspro.LoginAsync("CONS29", "", "UAT");
            var sessionId = responseXml.Body.LogonResult;
            //return Ok(new { response = responseXml.Body.LogonResult });
            // var sessionId = await LoginAsync("EDU", "ADMIN", "1234");


            string xmlIn = GenerateSortoiXml(existingMaster, existingOrderDetails);
            string parameters = GenerateSortoiParametersXml();
            var response = await syspro.Transaction(sessionId, "SORTSU", parameters, xmlIn);
            var response1 = await syspro.LogoutAsync(sessionId);

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
        private string GenerateSortRKParametersXml(bool validateOnly = false, bool ignoreWarnings = false)
        {
            //Declaration
            StringBuilder Document = new StringBuilder();

            //Building Document content
            Document.Append("<?xml version=\"1.0\" encoding=\"Windows-1252\"?>");
            Document.Append("<!-- Copyright 1994-2014 SYSPRO Ltd.-->");
            Document.Append("<!--");
            Document.Append("This is an example XML instance to demonstrate");
            Document.Append("use of the Sales Order Release Kit Quantities Business Object");
            Document.Append("-->");
            Document.Append("<PostSorKitRelease xmlns:xsd=\"http://www.w3.org/2001/XMLSchema-instance\" xsd:noNamespaceSchemaLocation=\"SORTRK.XSD\">");
            Document.Append("<Parameters>");
            Document.Append("<IgnoreWarnings>N</IgnoreWarnings>");
            Document.Append("<ApplyIfEntireDocumentValid>N</ApplyIfEntireDocumentValid>");
            Document.Append("<ValidateOnly>N</ValidateOnly>");
            Document.Append("<IgnoreAutoDepletion>N</IgnoreAutoDepletion>");
            Document.Append("</Parameters>");
            Document.Append("</PostSorKitRelease>");
            
         return Document.ToString();
        }
        private string GenerateSortRKXml(SorMaster master, List<SorDetail> lines, List<SorDetail> parentlines)
        {
            //Declaration
            StringBuilder Document = new StringBuilder();

            //Building Document content
            Document.Append("<?xml version=\"1.0\" encoding=\"Windows-1252\"?>");
            Document.Append("<PostSorKitRelease xmlns:xsd=\"http://www.w3.org/2001/XMLSchema-instance\"");
            Document.Append("xsd:noNamespaceSchemaLocation=\"SORTRK.XSD\">");
            Document.Append("<Parameters>");
            Document.Append("<IgnoreWarnings>N</IgnoreWarnings>");
            Document.Append("<ApplyIfEntireDocumentValid>N</ApplyIfEntireDocumentValid>");
            Document.Append("<ValidateOnly>N</ValidateOnly>");
            Document.Append("<IgnoreAutoDepletion>N</IgnoreAutoDepletion>");
            Document.Append("</Parameters>");
            foreach (var line in lines)
            {
                Document.Append("<Item>");
                Document.Append($"<SalesOrder>{master.SalesOrder}</SalesOrder>");
                Document.Append($"<SalesOrderLine>{line.SalesOrderLine}</SalesOrderLine>");
                Document.Append($"<ReleaseQuantity>{line.MorderQty}</ReleaseQuantity>");
                Document.Append($"<Warehouse>{line.Mwarehouse}</Warehouse>");
                Document.Append($"<StockCode>{line.MstockCode}</StockCode>");
                Document.Append($"<Bin>{parentlines[0].MstockCode}</Bin>");
                Document.Append("</Item>");
            }
           
            Document.Append("</PostSorKitRelease>");
            return Document.ToString();
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
            var routedClerk= _context.Users.FirstAsync(x => x.Username == username).Result.DefaultRouteClerk;
            if (routedClerk == null)
                return BadRequest("No default route clerk set for the user.Please reach out admin team.");

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
                status = "RepCompleted";

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
                        existing.RepUsageQty = dto.RepUsageQty??0;
                        existing.RepEntertedDate = DateTime.Now;
                        existing.RepName = username;
                        existing.RoutedClerk = routedClerk;
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
                                RoutedClerk = routedClerk,
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

        [HttpPost("reroute")]
        public async Task<IActionResult> UpdateReRoute([FromBody] RerouteRequestDto payload)
        {
            if (long.TryParse(payload.DeliveryNote, out long numericOrder))
            {
                // Format to 15 digits with leading zeros
                payload.DeliveryNote = numericOrder.ToString("D15");
            }

            var existing = await _context.DeliveryOrderDetails
        .Where(x => x.SalesOrder == payload.DeliveryNote).ToListAsync();

                    if (existing != null)
                    {
                        // Update existing
                        foreach(var  dto in existing)
                {
                    dto.RoutedClerk = payload.ClerkId.ToString();
                }
                        

                    }

               
                await _context.SaveChangesAsync();

            


            return Ok(new { message = "Saved successfully", count = 0 });

        }
        [HttpPost("revieworderdetails")]
        public async Task<IActionResult> GetAdminSalesOrders([FromBody] SalesOrderRequest request)
        {



            var order = await _sysContext.VwFetchSordetails.Where(o => o.OrderStatus == "4")
                .ToListAsync();
            if (!String.IsNullOrEmpty(request?.SalesOrderNumber))
            {
                if (long.TryParse(request.SalesOrderNumber, out long numericOrder))
                {
                    // Format to 15 digits with leading zeros
                    request.SalesOrderNumber = numericOrder.ToString("D15");
                }
                order = order
                    .Where(o => o.SalesOrder.Contains(request.SalesOrderNumber))
                    .ToList();

            }

            if (order?.Count == 0)
                return NotFound("Sales order not found.");

            return Ok(order);
        }


        [HttpGet("GetDeliveryNotes")]
        public async Task<IActionResult> GetDeliveryNotes()
        {



            var groupedOrders = await _sysContext.VwFetchSordetails
    .Where(o => o.OrderStatus == "4")
    .GroupBy(o => new { o.SalesOrder, o.Customer, o.Status,o.Salesperson ,o.Area})
    .Select(g => new
    {
        SalesOrder = g.Key.SalesOrder.TrimStart('0'),
        Customer = g.Key.Customer,
        Status = g.Key.Status,
        SalesPerson = g.Key.Salesperson,
        Area = g.Key.Area,
    })
    .ToListAsync();

            var roles = User.FindFirst(ClaimTypes.Role)?.Value;
            var salesPerson = User.FindFirst(ClaimTypes.GivenName)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (roles =="rep")
            {
                var areas = await _context.AreaMappings.Where(a => a.Username == username).Select(a => a.Area).ToListAsync();
                groupedOrders = groupedOrders.Where(o =>  areas.Contains(o.Area) &&  o.Status == null || o.Status == "Inprogress").ToList();
                
            }
            if (roles == "repclerk")
            {
                groupedOrders = groupedOrders.Where(o => o.Status == "RepCompleted" || o.Status == "StoresInProgress" || o.Status == "Completed&ReadyForValidation" || o.Status =="Send Email To Customer Service" || o.Status == "ReadyToPostSyspro").ToList();

            }
            if (groupedOrders?.Count == 0)
                return NotFound("Sales order not found.");

            return Ok(groupedOrders);
        }
        [HttpGet("GetDashBoard")]
        public async Task<IActionResult> GetDashBoard([FromQuery] string period = "currentMonth")
        {
            DateTime startDate = DateTime.MinValue;
            DateTime endDate = DateTime.Today;

            // Determine date range based on period
            switch (period.ToLower())
            {
                case "last7days":
                    startDate = DateTime.Today.AddDays(-7);
                    break;
                case "all":
                    startDate = DateTime.Today.AddYears(-1);
                    break;

                case "lastmonth":
                    var firstDayLastMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                    var lastDayLastMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
                    startDate = firstDayLastMonth;
                    endDate = lastDayLastMonth;
                    break;

                case "currentmonth":
                default:
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    endDate = DateTime.Today;
                    break;
            }

            // Fetch orders within date range
            var groupedOrders = await _sysContext.VwFetchSordetails
                .Where(o => o.OrderStatus == "4" && o.OrderDate >= startDate && o.OrderDate <= endDate)
                .Select(o => new
                {
                    SalesOrder = o.SalesOrder.TrimStart('0'),
                    Status = o.Status,
                    area= o.Area
                }).Distinct()
                .ToListAsync();

            // Role-based filtering (same as before)
            var roles = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

            if (roles == "rep")
            {
                var areas = await _context.AreaMappings
                    .Where(a => a.Username == username)
                    .Select(a => a.Area)
                    .ToListAsync();

                groupedOrders = groupedOrders
                    .Where(o => areas.Contains(o.area) &&
                                (o.Status == null || o.Status == "Inprogress" || o.Status == "RepCompleted"))
                    .ToList();
            }
            else if (roles == "repclerk")
            {
                groupedOrders = groupedOrders
                    .Where(o => o.Status == "RepCompleted"
                             || o.Status == "StoresInProgress"
                             || o.Status == "Completed&ReadyForValidation"
                             || o.Status == "Send Email To Customer Service"
                             || o.Status == "ReadyToPostSyspro")
                    .ToList();
            }

            if (!groupedOrders.Any())
                return NotFound("Sales order not found.");

            // Aggregate counts by Status
            var dashboardData = groupedOrders
                .GroupBy(o => o.Status ?? "Not Started")
                .Select(g => new
                {
                    name = g.Key,
                    value = g.Count()
                })
                .ToList();

            return Ok(dashboardData);
        }

        [HttpGet("GetSalesPerson")]
        public async Task<IActionResult> GetSalesPerson()
        {



            var groupedOrders = await _sysContext.SalSalespeople
    .ToListAsync();

            

            return Ok(groupedOrders);
        }
        [AllowAnonymous]
        [HttpGet("GetSalesArea")]
        public async Task<IActionResult> GetSalesArea()
        {



            var groupedOrders = await _sysContext.SalAreas
    .ToListAsync();



            return Ok(groupedOrders);
        }
        [HttpGet("GetClerks")]
        public async Task<IActionResult> GetClerks()
        {



            var groupedOrders = await _context.Users.Where(u => u.Roles == "repclerk").OrderBy(x => x.Username)
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
            if (long.TryParse(request.SalesOrderNumber, out long numericOrder))
            {
                // Format to 15 digits with leading zeros
                request.SalesOrderNumber = numericOrder.ToString("D15");
            }


            var order = await _sysContext.VwFetchSordetails.Where(o => o.OrderStatus == "4" && o.Status == "RepCompleted" || o.Status == "Send Email To Customer Service" || o.Status == "StoresInProgress" || o.Status == "Completed&ReadyForValidation" || o.Status == "ReadyToPostSyspro")
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

            if (long.TryParse(request.SalesOrderNumber, out long numericOrder))
            {
                // Format to 15 digits with leading zeros
                request.SalesOrderNumber = numericOrder.ToString("D15");
            }
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
                order = order.Where(o => o.Status == null || o.Status == "Inprogress").ToList();

            }
            if (roles == "repclerk")
            {
                order = order.Where(o => o.Status == "RepCompleted" || o.Status == "StoresInProgress" || o.Status == "Completed&ReadyForValidation" || o.Status== "Send Email To Customer Service" || o.Status == "ReadyToPostSyspro").ToList();

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
    public class DashboardRequest
    {
        public string Period { get; set; }
    }
    public class RerouteRequestDto
    {
        public string DeliveryNote { get; set; }
        public int ClerkId { get; set; }
    }
}
