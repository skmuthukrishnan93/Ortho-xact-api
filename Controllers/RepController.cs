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
using System.Reflection.Metadata;
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
            try
            {
                var items = payload.Data;
                if (items == null || !items.Any())
                    return BadRequest("No data received.");
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                // Group by SalesOrder
                var groupedBySalesOrder = items
                    .GroupBy(dto => dto.SalesOrder);

                SorMaster? existingMaster;
                List<SorDetail> existingOrderDetails;
                (bool flowControl, IActionResult value) = await UpdateSormaster(username, groupedBySalesOrder);
                if (!flowControl)
                {
                    return value;
                }
                //var syspro = new SysproWebService();
                //var responseXml = await syspro.LoginAsync("CONS29", "", "UAT");
                //var responseXml = await syspro.LoginAsync("CONS29", "", "OXZ");
                //var sessionId = responseXml.Body.LogonResult;
                //return Ok(new { response = responseXml.Body.LogonResult });
                // var sessionId = await LoginAsync("EDU", "ADMIN", "1234");


                //string xmlIn = GenerateSortoiXml(existingMaster, existingOrderDetails);
                //string parameters = GenerateSortoiParametersXml();
                //var response = await syspro.Transaction(sessionId, "SORTSU", parameters, xmlIn);
                //var response1 = await syspro.LogoutAsync(sessionId);

                return Ok(new { message = "Saved successfully", count = 0 });
            }
            catch (Exception ex)
            {
                // 🔥 TEMP: return full error
                return StatusCode(500, new
                {
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    inner = ex.InnerException?.Message
    });
            }
        }

        private async Task<(bool flowControl, IActionResult value)> UpdateSormaster(string? username, IEnumerable<IGrouping<string, DeliveryOrderDetailDto>> groupedBySalesOrder)
        {
           var existingMaster = new SorMaster();
         var   existingOrderDetails = new List<SorDetail>();
            var parentOrderDetails = new List<SorDetail>();
            var sorDetailBin = new List<SorDetailBin>();
            foreach (var group in groupedBySalesOrder)
            {
                var group1 = group.Where(c => c.MbomFlag != "P");
                bool allHaveQty = group1.All(x => x.RepUsageQty.HasValue);
                bool anyHaveQty = group1.Any(x => x.RepUsageQty.HasValue);
                if (!allHaveQty)
                {
                    return (flowControl: false, value: BadRequest("Validation Failed"));
                }
                var salesorder = group.FirstOrDefault()?.SalesOrder;
                existingMaster = await _sysContext.SorMasters.Where(c => c.SalesOrder == salesorder).FirstOrDefaultAsync();
                existingOrderDetails = await _sysContext.SorDetails.Where(c => c.SalesOrder == salesorder && c.MbomFlag != "P").ToListAsync();
                parentOrderDetails = await _sysContext.SorDetails.Where(c => c.SalesOrder == salesorder && c.MbomFlag == "P").ToListAsync();
                sorDetailBin = await _sysContext.SorDetailBins.Where(c => c.SalesOrder == salesorder).ToListAsync();
                var allMultiBins = await _sysContext.InvMultBins.ToListAsync();
                string status = "PostedToSyspro";
                existingMaster.OrderStatus = "1";
                foreach (var dto in group)
                {
                    var dtls = existingOrderDetails.FirstOrDefault(x => x.SalesOrder == dto.SalesOrder && x.SalesOrderLine == dto.SalesOrderLine && x.MbomFlag != "P");
                    var dtlsBin = sorDetailBin.FirstOrDefault(x => x.SalesOrderLine == dto.SalesOrderLine);
                    if (dtls != null)
                    {
                        dtls.MorderQty = (decimal)dto.Usage;
                        dtls.MshipQty = (decimal)dto.Usage;
                        dtls.MstockQtyToShp = (decimal)dto.Usage;
                        dtls.MqtyPer = (decimal)dto.Usage;
                        dtls.MstockUnitMass = (decimal)dto.Usage;
                        if (dto.Usage == 0)
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
                        dtlsBin.QtyReserved = (decimal)dto.Usage;
                        // dtlsBin.Bin = parentOrderDetails.FirstOrDefault()?.MstockCode ?? string.Empty;
                    }
                    if (dtls != null)
                    {
                        if (!string.IsNullOrWhiteSpace(dtls.Mbin) &&
                            !string.IsNullOrWhiteSpace(dtls.Mwarehouse) &&
                            !string.IsNullOrWhiteSpace(dtls.MstockCode))
                        {
                            var existMultiBin = allMultiBins.FirstOrDefault(c =>
                                string.Equals((c.Bin ?? "").Trim(), dtls.Mbin.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                string.Equals((c.Warehouse ?? "").Trim(), dtls.Mwarehouse.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                string.Equals((c.StockCode ?? "").Trim(), dtls.MstockCode.Trim(), StringComparison.OrdinalIgnoreCase)
                            );

                            if (existMultiBin != null)
                            {
                                existMultiBin.SoQtyToShip = (decimal)dto.Usage;
                            }
                        }
                    }

                }
            }
            await _sysContext.SaveChangesAsync();
            await _context.SaveChangesAsync();
            return (flowControl: true, value: null);
        }
        private async Task<(bool flowControl, IActionResult value)> UpdateStatus(string? username, IEnumerable<IGrouping<string, DeliveryOrderDetailDto>> groupedBySalesOrder)
        {
          var  existingMaster = new SorMaster();
          var  existingOrderDetails = new List<SorDetail>();
            var parentOrderDetails = new List<SorDetail>();
            var sorDetailBin = new List<SorDetailBin>();
            foreach (var group in groupedBySalesOrder)
            {
                var group1 = group.Where(c => c.MbomFlag != "P");
                bool allHaveQty = group1.All(x => x.RepUsageQty.HasValue);
                bool anyHaveQty = group1.Any(x => x.RepUsageQty.HasValue);
                
                var salesorder = group.FirstOrDefault()?.SalesOrder;
                existingMaster = await _sysContext.SorMasters.Where(c => c.SalesOrder == salesorder).FirstOrDefaultAsync();
                existingOrderDetails = await _sysContext.SorDetails.Where(c => c.SalesOrder == salesorder && c.MbomFlag != "P").ToListAsync();
                parentOrderDetails = await _sysContext.SorDetails.Where(c => c.SalesOrder == salesorder && c.MbomFlag == "P").ToListAsync();
                sorDetailBin = await _sysContext.SorDetailBins.Where(c => c.SalesOrder == salesorder).ToListAsync();

                string status = "PostedToSyspro";
                existingMaster.OrderStatus = "1";
                foreach (var dto in group)
                {
                    var dtls = existingOrderDetails.FirstOrDefault(x => x.SalesOrder == dto.SalesOrder && x.SalesOrderLine == dto.SalesOrderLine && x.MbomFlag != "P");
                    var dtlsBin = sorDetailBin.FirstOrDefault(x => x.SalesOrderLine == dto.SalesOrderLine);
                    if (dtls != null)
                    {
                        dtls.MshipQty = (decimal)dto.Usage;
                        dtls.MstockQtyToShp = (decimal)dto.Usage;
                        dtls.MqtyPer = (decimal)dto.Usage;
                        dtls.MstockUnitMass = (decimal)dto.Usage;

                    }
                   
                   
                   

                }
            }
            await _sysContext.SaveChangesAsync();
            return (flowControl: true, value: null);
        }

        [HttpPost("posttosyspro1")]
        public async Task<IActionResult> SysproPost([FromBody] DeliveryOrderDetailPayload payload)
    {
        try
        {
            var items = payload.Data;

            if (items == null || !items.Any())
                return BadRequest("No data received");

            // ============================================
            // GROUP BY SALES ORDER
            // ============================================
            var groupedOrders = items.GroupBy(x => x.SalesOrder);

            // ============================================
            // LOGIN TO SYSPRO
            // ============================================
            var syspro = new SysproWebService();

            var loginResponse = await syspro.LoginAsync(
                "CONS29",
                "",
                "UAT"
            );

            string sessionId = loginResponse.Body.LogonResult;

            foreach (var orderGroup in groupedOrders)
            {
                string salesOrder = orderGroup.Key;

                // =====================================================
                // STEP 1 : QUERY CURRENT SALES ORDER
                // =====================================================
                string queryXml = GenerateSorQueryXml(salesOrder);

                string queryParam = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Parameters>
    <Key>
        <SalesOrder>" + salesOrder + @"</SalesOrder>
    </Key>
</Parameters>";

                var queryResponse = await syspro.Query(
                    sessionId,
                    "SORQRY",
                    queryParam,
                    queryXml
                );
                    var groupedBySalesOrder = items
                    .GroupBy(dto => dto.SalesOrder);

                    SorMaster? existingMaster;
                    List<SorDetail> existingOrderDetails;
                    (bool flowControl, IActionResult value) = await UpdateStatus("username", groupedBySalesOrder);
                    if (!flowControl)
                    {
                        return value;
                    }
                    // =====================================================
                    // STEP 2 : BUILD SORTSU XML
                    // =====================================================

                    string reverseXml= BuildReverseDispatchXml(orderGroup.ToList());
                    string reverseParamxml = BuildSortDnParameterXml();

                    var response1 = await syspro.Transaction(
       sessionId,
       "SORTDN",
       reverseParamxml,
       reverseXml
   );

                    // =====================================================
                    // HANDLE ERRORS
                    // =====================================================
                    string result1 = response1.Body.PostResult;
                    string xmlIn = GenerateKitComponentXml(orderGroup.ToList());

                string parameterXml = GenerateSortsuParameterXml();

                    // =====================================================
                    // STEP 3 : CALL SORTSU
                    // =====================================================
                    var response = await syspro.Transaction(
       sessionId,
       "SORTSU",
       parameterXml,
       xmlIn
   );

                    // =====================================================
                    // HANDLE ERRORS
                    // =====================================================
                    string result = response.Body.PostResult;

                if (result.Contains("<ErrorDescription>"))
                {
                    return BadRequest(result);
                }

                // =====================================================
                // UPDATE YOUR CUSTOM TABLE ONLY
                // =====================================================
                foreach (var dto in orderGroup)
                {
                    var existing = await _context.DeliveryOrderDetails
                        .FirstOrDefaultAsync(x =>
                            x.SalesOrder == dto.SalesOrder &&
                            x.Line == dto.SalesOrderLine);

                    if (existing != null)
                    {
                        existing.Status = "PostedToSyspro";
                        existing.PostedDate = DateTime.Now;
                    }
                }
            }

            await _context.SaveChangesAsync();

            // ============================================
            // LOGOUT
            // ============================================
            await syspro.LogoutAsync(sessionId);

            return Ok(new
            {
                message = "Successfully posted to SYSPRO"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = ex.Message,
                inner = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    private string GenerateSortsuParameterXml()
    {
            StringBuilder sb = new StringBuilder();

            sb.Append("<?xml version=\"1.0\" encoding=\"Windows-1252\"?>");

            sb.Append("<PostChangeSalesOrderKitComp ");
            sb.Append("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema-instance\" ");
            sb.Append("xsd:noNamespaceSchemaLocation=\"SORTSU.XSD\">");

            sb.Append("<Parameters>");

            sb.Append("<IgnoreWarnings>N</IgnoreWarnings>");
            sb.Append("<ApplyIfEntireDocumentValid>N</ApplyIfEntireDocumentValid>");
            sb.Append("<ValidateOnly>N</ValidateOnly>");

            sb.Append("</Parameters>");

            sb.Append("</PostChangeSalesOrderKitComp>");

            return sb.ToString();
        }

    private string GenerateSorQueryXml(string salesOrder)
    {
        return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Query>
    <Key>
        <SalesOrder>" + salesOrder + @"</SalesOrder>
    </Key>
</Query>";
    }
        private string BuildSortDnParameterXml()
        {
            StringBuilder xml = new StringBuilder();

            xml.AppendLine(@"<?xml version=""1.0"" encoding=""Windows-1252""?>");

            xml.AppendLine(@"
<PostDispatchNotes
    xmlns:xsd=""http://www.w3.org/2001/XMLSchema-instance""
    xsd:noNamespaceSchemaLocation=""SORTDN.XSD"">");

            xml.AppendLine(@"  <Parameters>");

            // Current Period
            xml.AppendLine(@"      <PostingPeriod>C</PostingPeriod>");

            // N = Actual Posting
            xml.AppendLine(@"      <ValidateOnly>N</ValidateOnly>");

            // Ignore Warnings
            xml.AppendLine(@"      <IgnoreWarnings>Y</IgnoreWarnings>");

            // Dispatch Basis
            xml.AppendLine(@"      <BasisForDispatch>B</BasisForDispatch>");

            // Inventory Source
            xml.AppendLine(@"      <NonMerchandiseSource>I</NonMerchandiseSource>");

            // Auto Depletion
            xml.AppendLine(@"      <IgnoreAutoDepletion>N</IgnoreAutoDepletion>");

            // Retain Zero Cost
            xml.AppendLine(@"      <RetainZeroNonMerchCost>N</RetainZeroNonMerchCost>");

            // Copy Custom Form
            xml.AppendLine(@"      <CopyCustomForm>N</CopyCustomForm>");

            // Append Existing Line
            xml.AppendLine(@"      <AppendToExistingLine>N</AppendToExistingLine>");

            // Default Warehouse
            xml.AppendLine(@"      <DefaultWhForNonStocked />");

            xml.AppendLine(@"  </Parameters>");

            xml.AppendLine(@"</PostDispatchNotes>");

            return xml.ToString();
        }
        private string BuildReverseDispatchXml(List<DeliveryOrderDetailDto> items)
        {
            StringBuilder xml = new StringBuilder();

            xml.AppendLine(@"<?xml version=""1.0"" encoding=""Windows-1252""?>");

            xml.AppendLine(@"
<PostDispatchNotes
    xmlns:xsd=""http://www.w3.org/2001/XMLSchema-instance""
    xsd:noNamespaceSchemaLocation=""SORTDN.XSD"">");

            // PARAMETERS
            xml.AppendLine(@"  <Parameters>");
            xml.AppendLine(@"      <PostingPeriod>C</PostingPeriod>");
            xml.AppendLine(@"      <ValidateOnly>N</ValidateOnly>");
            xml.AppendLine(@"      <IgnoreWarnings>Y</IgnoreWarnings>");
            xml.AppendLine(@"      <BasisForDispatch>B</BasisForDispatch>");
            xml.AppendLine(@"      <NonMerchandiseSource>I</NonMerchandiseSource>");
            xml.AppendLine(@"      <IgnoreAutoDepletion>N</IgnoreAutoDepletion>");
            xml.AppendLine(@"      <RetainZeroNonMerchCost>N</RetainZeroNonMerchCost>");
            xml.AppendLine(@"      <CopyCustomForm>N</CopyCustomForm>");
            xml.AppendLine(@"      <AppendToExistingLine>N</AppendToExistingLine>");
            xml.AppendLine(@"      <DefaultWhForNonStocked/>");
            xml.AppendLine(@"  </Parameters>");

            // DISPATCH NOTE
            xml.AppendLine(@"  <DispatchNote>");

            // HEADER
            xml.AppendLine(@"      <DispatchHeader>");
            xml.AppendLine($@"          <SalesOrder>{items[0].SalesOrder}</SalesOrder>");
            xml.AppendLine(@"      </DispatchHeader>");

            // DETAILS
            xml.AppendLine(@"      <DispatchDetails>");

            foreach (var item in items.Where(c => c.MbomFlag != "P"))
            {
                xml.AppendLine(@"          <MerchandiseLine>");

                // SALES ORDER LINE
                xml.AppendLine($@"              <SalesOrderLine>{item.SalesOrderLine}</SalesOrderLine>");

                // NEGATIVE QTY TO REVERSE
                xml.AppendLine($@"              <DispatchQty>{item.Usage}</DispatchQty>");

                xml.AppendLine(@"              <Units />");
                xml.AppendLine(@"              <Pieces />");

                // BIN DETAILS
                xml.AppendLine(@"              <Bins>");
                xml.AppendLine($@"                  <BinLocation>{item.Mwarehouse}</BinLocation>");
                xml.AppendLine($@"                  <BinQuantity>{item.Usage}</BinQuantity>");
                xml.AppendLine(@"              </Bins>");

                xml.AppendLine(@"              <BasisForDispatch>B</BasisForDispatch>");

                xml.AppendLine(@"          </MerchandiseLine>");
            }

            xml.AppendLine(@"      </DispatchDetails>");

            xml.AppendLine(@"  </DispatchNote>");

            xml.AppendLine(@"</PostDispatchNotes>");

            return xml.ToString();
        }

        private string GenerateKitComponentXml(List<DeliveryOrderDetailDto> items)
        {
            //Declaration
            StringBuilder Document = new StringBuilder();

            //Building Document content
            Document.Append("<?xml version=\"1.0\" encoding=\"Windows-1252\"?>");
            Document.Append("<!-- Copyright 1994-2019 SYSPRO Ltd.-->");
            Document.Append("<!--");
            Document.Append("This is an example XML instance to demonstrate");
            Document.Append("use of the SO Sales Order Change Kit Component Line Business Object");
            Document.Append("-->");
            Document.Append("<PostChangeSalesOrderKitComp xmlns:xsd=\"http://www.w3.org/2001/XMLSchema-instance\" xsd:noNamespaceSchemaLocation=\"SORTSUDOC.XSD\">");
            foreach (var item in items.Where(c => c.MbomFlag !="P"))
            {
                Document.Append("<Item>");
                Document.Append($"<SalesOrder>{item.SalesOrder}</SalesOrder>");
                Document.Append($"<SalesOrderLine>{item.SalesOrderLine}</SalesOrderLine >");
                Document.Append($"<Customer>{item.Customer}</Customer>");
                Document.Append($"<StockCode>{item.MstockCode}</StockCode>");
                Document.Append("<Description/>");
                Document.Append($"<OrderQty>{item.Usage}</OrderQty>");
                Document.Append($"<ShipQty>{item.Usage}</ShipQty>");
                Document.Append($"<QtyPer>{item.Usage}</QtyPer>"); 

                // Required in many setups
                Document.Append($"<ParentLine>1</ParentLine>");

                Document.Append("<Component>Y</Component>");

                Document.Append($"<Warehouse>{item.Mwarehouse}</Warehouse>");
                Document.Append("<QuantityType/>");
                Document.Append("<ProductClass/>");
                Document.Append("<PriceGroupRule></PriceGroupRule>");
                Document.Append("	<Catalog></Catalog>");
                Document.Append("	<CatalogLine></CatalogLine>");
                Document.Append("<DiscPercent1>0</DiscPercent1>");
                Document.Append("<DiscPercent2>0</DiscPercent2>");
                Document.Append("<DiscPercent3>0</DiscPercent3>");
                Document.Append("<DiscValue/>");
                Document.Append("<DiscValFlag/>");
                Document.Append("<eSignature/>");
                Document.Append("</Item>");
            }
            Document.Append("</PostChangeSalesOrderKitComp>");
            return Document.ToString();
        }
        [HttpPost("posttosyspro2")]
        public async Task<IActionResult> SysproPostBusinessObject(
    [FromBody] DeliveryOrderDetailPayload payload)
        {
            StringBuilder warnMessages = new StringBuilder();
            var items = payload.Data;
            try
            {
                

                if (items == null || !items.Any())
                    return BadRequest("No data received");

                // ============================================
                // GROUP SALES ORDERS
                // ============================================
                var groupedOrders = items.GroupBy(x => x.SalesOrder);

                // ============================================
                // LOGIN TO SYSPRO
                // ============================================
                var syspro = new SysproWebService();

                var loginResponse = await syspro.LoginAsync(
                    "CONS29",
                    "",
                    "OXZ"
                );

                string sessionId = loginResponse.Body.LogonResult;

                // ============================================
                // PROCESS EACH SALES ORDER
                // ============================================
                foreach (var orderGroup in groupedOrders)
                {
                    string salesOrder = orderGroup.Key;

                    // ============================================
                    // GET EXISTING MASTER
                    // ============================================
                    var existingMaster = await _sysContext.SorMasters
                        .Where(c => c.SalesOrder == salesOrder)
                        .FirstOrDefaultAsync();

                    if (existingMaster == null)
                    {
                        return BadRequest(
                            $"Sales order not found : {salesOrder}");
                    }
                    existingMaster.OrderStatus = "1";

                    await _sysContext.SaveChangesAsync();
                    _sysContext.ChangeTracker.Clear();
                    // ============================================
                    // GET EXISTING DETAILS
                    // ============================================
                    var existingOrderDetails = await _sysContext.SorDetails
                        .Where(c =>
                            c.SalesOrder == salesOrder &&
                            c.MbomFlag != "P")
                        .ToListAsync();

                    var newSalesList = new List<SorDetail>();
                    var newDtoList = new List<DeliveryOrderDetailDto>();
                    // ============================================
                    // VALIDATION
                    // ============================================
                    var delivery = await _context.DeliveryOrderDetails.Where(c => c.SalesOrder == salesOrder && c.Set !=c.MstockCode).ToListAsync();

                    if (!existingOrderDetails.Any())
                    {
                        foreach(var item in delivery)
                        {
                            var newOrderDtls = new SorDetail();
                            var newDTO = new DeliveryOrderDetailDto();
                            newOrderDtls.SalesOrder = item.SalesOrder;
                            newOrderDtls.SalesOrderLine = item.Line;
                            newOrderDtls.MstockCode = item.MstockCode;
                            newOrderDtls.MstockDes = item.MstockDes;
                            newOrderDtls.MorderUom = "EA";
                            newOrderDtls.MpriceUom = "EA";
                            newOrderDtls.Mprice = 0;
                            newSalesList.Add(newOrderDtls);

                            newDTO.SalesOrder = item.SalesOrder;
                            newDTO.SalesOrderLine = item.Line;
                            newDTO.MstockCode = item.MstockCode;
                            newDTO.MstockDes = item.MstockDes;
                            newDTO.RepUsageQty= item.RepUsageQty;
                            newDTO.Usage = item.Usage;
                            newDTO.Variance = item.Variance;
                            newDTO.Mwarehouse = item.Mwarehouse;
                            newDTO.MbomFlag = "C";
                            newDtoList.Add(newDTO);
                        }
                       
                        // return BadRequest(
                        //    $"No SO detail lines found : {salesOrder}");
                    }
                    //var linesToProcess = new List<SorDetail>();

                    //foreach (var dto in orderGroup)
                    //{
                    //    var sorLine = existingOrderDetails
                    //        .FirstOrDefault(x =>
                    //            x.MstockCode == dto.MstockCode);

                    //    if (sorLine == null)
                    //        continue;

                    //    // Skip if Usage already equals ShipQty
                    //    if (dto.Usage == sorLine.MshipQty)
                    //        continue;

                    //    linesToProcess.Add(sorLiner);
                    //}
                    // ============================================
                    // STEP 1
                    // CALL SORTOX ONLY ONE TIME
                    // ============================================
                    if (existingOrderDetails.Any())
                    {
                        string sortoxParameter =
                            BuildSortoxParameterXml();

                        const int batchSize = 10;

                        for (int i = 0; i < existingOrderDetails.Count; i += batchSize)
                        {
                            var batchDetails = existingOrderDetails
                                .Skip(i)
                                .Take(batchSize)
                                .ToList();

                            string sortoxDocument =
                                BuildSortoxDocumentXml(
                                    salesOrder,
                                    batchDetails,
                                    "02"
                                );

                            var cancelResponse =
                                await syspro.Transaction(
                                    sessionId,
                                    "SORTOX",
                                    sortoxParameter,
                                    sortoxDocument
                                );

                            string cancelResult =
                                cancelResponse.Body.PostResult;

                            await SaveSysproLog(
                                salesOrder,
                                $"SORTOX_BATCH_{(i / batchSize) + 1}",
                                sortoxDocument,
                                cancelResult
                            );

                            if (cancelResult.Contains("<ErrorDescription>"))
                            {
                                existingMaster.OrderStatus = "4";

                                await _sysContext.SaveChangesAsync();

                                return BadRequest(new
                                {
                                    message = $"SORTOX Batch {(i / batchSize) + 1} Failed",
                                    salesOrder,
                                    response = cancelResult
                                });
                            }
                        }
                    }
                    // ============================================
                    // STEP 2
                    // CALL SORTOI ONLY ONE TIME
                    // ============================================
                    string sortoiParameter =
                        BuildSortoiParameterXml();

                    if (existingOrderDetails.Any())
                    {
                        var orderLines = orderGroup.ToList();

                        for (int i = 0; i < orderLines.Count; i += 20)
                        {
                            var batch = orderLines
                                .Skip(i)
                                .Take(20)
                                .ToList();

                            string sortoiDocument =
                                BuildSortoiDocumentXml(
                                    batch,
                                    existingMaster,
                                    existingOrderDetails
                                );

                            var addResponse =
                                await syspro.Transaction(
                                    sessionId,
                                    "SORTOI",
                                    sortoiParameter,
                                    sortoiDocument
                                );

                            string addResult = addResponse.Body.PostResult;

                            await SaveSysproLog(
                                salesOrder,
                                $"SORTOI_BATCH_{(i / 10) + 1}",
                                sortoiDocument,
                                addResult
                            );

                            if (addResult.Contains("<ErrorDescription>"))
                            {
                                existingMaster.OrderStatus = "4";

                                await _sysContext.SaveChangesAsync();

                                return BadRequest(new
                                {
                                    message = $"SORTOI Batch {(i / 10) + 1} Failed",
                                    salesOrder,
                                    response = addResult
                                });
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < newDtoList.Count; i += 10)
                        {
                            var batchDtos = newDtoList
                                .Skip(i)
                                .Take(10)
                                .ToList();

                            var batchSales = newSalesList
                                .Skip(i)
                                .Take(10)
                                .ToList();

                            string sortoiDocument =
                                BuildSortoiDocumentXml(
                                    batchDtos,
                                    existingMaster,
                                    batchSales
                                );

                            var addResponse =
                                await syspro.Transaction(
                                    sessionId,
                                    "SORTOI",
                                    sortoiParameter,
                                    sortoiDocument
                                );

                            string addResult = addResponse.Body.PostResult;

                            await SaveSysproLog(
                                salesOrder,
                                $"SORTOI_BATCH_{(i / 10) + 1}",
                                sortoiDocument,
                                addResult
                            );

                            if (addResult.Contains("<ErrorDescription>"))
                            {
                                existingMaster.OrderStatus = "4";

                                await _sysContext.SaveChangesAsync();

                                return BadRequest(new
                                {
                                    message = $"SORTOI Batch {(i / 10) + 1} Failed",
                                    salesOrder,
                                    response = addResult
                                });
                            }
                        }
                    }

                    // ============================================
                    // STEP 3
                    // UPDATE MBOMFLAG
                    // ============================================
                    var updateDetails = await _sysContext.SorDetails
                        .Where(c =>
                            c.SalesOrder == salesOrder &&
                            c.MbomFlag != "P")
                        .ToListAsync();
                    foreach (var detail in updateDetails)
                    {
                        
                        if (detail.MbackOrderQty > 0)
                        {
                            var deliveryRecord =
                                await _context.DeliveryOrderDetails
                                .FirstOrDefaultAsync(x =>
                                    x.SalesOrder == detail.SalesOrder &&
                                    x.MstockCode == detail.MstockCode);

                            var bin = await _sysContext.InvMultBins.Where(c =>  c.Bin == deliveryRecord.Set && c.StockCode == detail.MstockCode).OrderByDescending(t => t.QtyOnHand1).FirstOrDefaultAsync();
                            if (bin != null)
                            {
                                var availableBin = bin.Bin;
                                if (availableBin != null)
                                {
                                    bin.SoQtyToShip = 0;
                                    await _sysContext.SaveChangesAsync();
                                }
                            }
                        }
                    }
                    _sysContext.ChangeTracker.Clear();
                    

                    foreach (var detail in updateDetails)
                    {
                        detail.MbomFlag = "C";
                        detail.MparentKitType = "K";
                        if(detail.MbackOrderQty>0)
                        {
                            var deliveryRecord =
                                await _context.DeliveryOrderDetails
                                .FirstOrDefaultAsync(x =>
                                    x.SalesOrder == detail.SalesOrder &&
                                    x.MstockCode == detail.MstockCode);

                            var bin = await _sysContext.InvMultBins.Where(c => c.QtyOnHand1 >= detail.MbackOrderQty && c.Bin == deliveryRecord.Set && c.StockCode==detail.MstockCode).OrderByDescending(t => t.QtyOnHand1).FirstOrDefaultAsync();
                            if (bin != null)
                            {
                                var availableBin = bin.Bin;
                                if (availableBin != null)
                                {
                                    string sortboParameter =
                        SortBoParamxml();

                                    string sortBoxmlIn =
                                        SortBoXml(availableBin,
                                            detail
                                        );

                                    var sortBoResponse =
                                        await syspro.Transaction(
                                            sessionId,
                                            "SORTBO",
                                            sortboParameter,
                                            sortBoxmlIn
                                        );
                                    await SaveSysproLog(
    salesOrder,
    "SORTBO",
    sortBoxmlIn,
    sortBoResponse.Body.PostResult
);
                                }
                            }
                        }
                    }
                    _sysContext.ChangeTracker.Clear();
                    _context.ChangeTracker.Clear();
                    var updateDetails1 = await _sysContext.SorDetails
                        .Where(c =>
                            c.SalesOrder == salesOrder &&
                            c.MbomFlag != "P")
                        .ToListAsync();
                   
                    foreach (var detail in updateDetails1)
                    {
                        detail.MbomFlag = "C";
                        detail.MparentKitType = "K";
                        detail.MqtyChangesFlag = "Y";
                    }
                        await _sysContext.SaveChangesAsync();
                    // ============================================
                    // OPTIONAL
                    // UPDATE DELIVERY TABLE
                    // ============================================
                    foreach (var dto in orderGroup)
                    {
                        //var newLine = updateDetails1
                        //    .Where(x =>
                        //        x.SalesOrder == dto.SalesOrder &&
                        //        x.MstockCode == dto.MstockCode)
                        //    .OrderByDescending(x => x.SalesOrderLine)
                        //    .FirstOrDefault();

                        //if (newLine != null)
                        //{
                            var deliveryRecord =
                                await _context.DeliveryOrderDetails
                                .FirstOrDefaultAsync(x =>
                                    x.SalesOrder == dto.SalesOrder &&
                                    x.MstockCode == dto.MstockCode);

                            if (deliveryRecord != null)
                            {
                               // deliveryRecord.Line = Convert.ToInt32(newLine.SalesOrderLine);
                                deliveryRecord.Status = "PostedToSyspro";
                                deliveryRecord.PostedDate = DateTime.Now;
                            }
                        //}
                    }
                }

                // ============================================
                // SAVE DATABASE
                // ============================================
                await _context.SaveChangesAsync();

                // ============================================
                // LOGOUT
                // ============================================
                await syspro.LogoutAsync(sessionId);
                string allErrors = "";
                if (warnMessages.Length > 0)
                {
                     allErrors = warnMessages.ToString();
                    // Log, return, or throw exception
                }
                return Ok(new
                {
                    message = "Successfully posted to SYSPRO " 

                });
            }
            catch (Exception ex)
            {
                await SaveSysproLog(
                                items.FirstOrDefault()?.SalesOrder,
                                $"Exception",
                                ex.Message.ToString(),
                                ex.StackTrace.ToString()
                            );
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        [HttpPost("posttosyspro3")]
        public async Task<IActionResult> SysproPostBusinessObject2(
    [FromBody] DeliveryOrderDetailPayload payload)
        {
            StringBuilder warnMessages = new StringBuilder();
            var items = payload.Data;

            try
            {
               
                if (items == null || !items.Any())
                    return BadRequest("No data received");

                // ============================================
                // GROUP SALES ORDERS
                // ============================================
                var groupedOrders = items.GroupBy(x => x.SalesOrder);

                // ============================================
                // LOGIN TO SYSPRO
                // ============================================
                var syspro = new SysproWebService();

                var loginResponse = await syspro.LoginAsync(
                    "CONS29",
                    "",
                    "OXZ"
                );

                string sessionId = loginResponse.Body.LogonResult;

                // ============================================
                // PROCESS EACH SALES ORDER
                // ============================================
                foreach (var orderGroup in groupedOrders)
                {
                    string salesOrder = orderGroup.Key;

                    // ============================================
                    // GET EXISTING MASTER
                    // ============================================
                    var existingMaster = await _sysContext.SorMasters
                        .Where(c => c.SalesOrder == salesOrder)
                        .FirstOrDefaultAsync();

                    if (existingMaster == null)
                    {
                        return BadRequest(
                            $"Sales order not found : {salesOrder}");
                    }
                    existingMaster.OrderStatus = "1";

                    await _sysContext.SaveChangesAsync();
                    _sysContext.ChangeTracker.Clear();
                    // ============================================
                    // GET EXISTING DETAILS
                    // ============================================
                    var existingOrderDetails = await _sysContext.SorDetails
                        .Where(c =>
                            c.SalesOrder == salesOrder &&
                            c.MbomFlag != "P")
                        .OrderBy(c => c.SalesOrderLine)
                        .ToListAsync();

                    var newSalesList = new List<SorDetail>();
                    var newDtoList = new List<DeliveryOrderDetailDto>();
                    // ============================================
                    // VALIDATION
                    // ============================================
                    var delivery = await _context.DeliveryOrderDetails.Where(c => c.SalesOrder == salesOrder && c.Set != c.MstockCode).ToListAsync();

                    if (!existingOrderDetails.Any())
                    {
                        foreach (var item in delivery)
                        {
                            var newOrderDtls = new SorDetail();
                            var newDTO = new DeliveryOrderDetailDto();
                            newOrderDtls.SalesOrder = item.SalesOrder;
                            newOrderDtls.SalesOrderLine = item.Line;
                            newOrderDtls.MstockCode = item.MstockCode;
                            newOrderDtls.MstockDes = item.MstockDes;
                            newOrderDtls.MorderUom = "EA";
                            newOrderDtls.MpriceUom = "EA";
                            newOrderDtls.Mprice = 0;
                            newSalesList.Add(newOrderDtls);

                            newDTO.SalesOrder = item.SalesOrder;
                            newDTO.SalesOrderLine = item.Line;
                            newDTO.MstockCode = item.MstockCode;
                            newDTO.MstockDes = item.MstockDes;
                            newDTO.RepUsageQty = item.RepUsageQty;
                            newDTO.Usage = item.Usage;
                            newDTO.Variance = item.Variance;
                            newDTO.Mwarehouse = item.Mwarehouse;
                            newDTO.MbomFlag = "C";
                            newDtoList.Add(newDTO);
                        }

                        // return BadRequest(
                        //    $"No SO detail lines found : {salesOrder}");
                    }
                    //var linesToProcess = new List<SorDetail>();

                    //foreach (var dto in orderGroup)
                    //{
                    //    var sorLine = existingOrderDetails
                    //        .FirstOrDefault(x =>
                    //            x.MstockCode == dto.MstockCode);

                    //    if (sorLine == null)
                    //        continue;

                    //    // Skip if Usage already equals ShipQty
                    //    if (dto.Usage == sorLine.MshipQty)
                    //        continue;

                    //    linesToProcess.Add(sorLiner);
                    //}
                    // ============================================
                    // STEP 1
                    // CALL SORTOX ONLY ONE TIME
                    // ============================================
                    if (existingOrderDetails.Any())
                    {
                        string sortoxParameter =
                            BuildSortoxParameterXml();

                        const int batchSize = 20;

                        for (int i = 0; i < existingOrderDetails.Count; i += batchSize)
                        {
                            var batchDetails = existingOrderDetails
                                .Skip(i)
                                .Take(batchSize)
                                .ToList();

                            string sortoxDocument =
                                BuildSortoxDocumentXml(
                                    salesOrder,
                                    batchDetails,
                                    "02"
                                );

                            var cancelResponse =
                                await syspro.Transaction(
                                    sessionId,
                                    "SORTOX",
                                    sortoxParameter,
                                    sortoxDocument
                                );

                            string cancelResult =
                                cancelResponse.Body.PostResult;

                            await SaveSysproLog(
                                salesOrder,
                                $"SORTOX_BATCH_{(i / batchSize) + 1}",
                                sortoxDocument,
                                cancelResult
                            );

                            if (cancelResult.Contains("<ErrorDescription>"))
                            {
                                existingMaster.OrderStatus = "4";

                                await _sysContext.SaveChangesAsync();

                                return BadRequest(new
                                {
                                    message = $"SORTOX Batch {(i / batchSize) + 1} Failed",
                                    salesOrder,
                                    response = cancelResult
                                });
                            }
                        }
                    }
                    // ============================================
                    // STEP 2
                    // CALL SORTOI ONLY ONE TIME
                    // ============================================
                    string sortoiParameter =
                        BuildSortoiParameterXml();

                    if (existingOrderDetails.Any())
                    {
                        var orderLines = orderGroup.ToList();

                        for (int i = 0; i < orderLines.Count; i += 20)
                        {
                            var batch = orderLines
                                .Skip(i)
                                .Take(20)
                                .ToList();

                            string sortoiDocument =
                                BuildSortoiDocumentXml(
                                    batch,
                                    existingMaster,
                                    existingOrderDetails
                                );

                            var addResponse =
                                await syspro.Transaction(
                                    sessionId,
                                    "SORTOI",
                                    sortoiParameter,
                                    sortoiDocument
                                );

                            string addResult = addResponse.Body.PostResult;

                            await SaveSysproLog(
                                salesOrder,
                                $"SORTOI_BATCH_{(i / 20) + 1}",
                                sortoiDocument,
                                addResult
                            );

                            if (addResult.Contains("<ErrorDescription>"))
                            {
                                existingMaster.OrderStatus = "4";

                                await _sysContext.SaveChangesAsync();

                                return BadRequest(new
                                {
                                    message = $"SORTOI Batch {(i / 20) + 1} Failed",
                                    salesOrder,
                                    response = addResult
                                });
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < newDtoList.Count; i += 10)
                        {
                            var batchDtos = newDtoList
                                .Skip(i)
                                .Take(10)
                                .ToList();

                            var batchSales = newSalesList
                                .Skip(i)
                                .Take(10)
                                .ToList();

                            string sortoiDocument =
                                BuildSortoiDocumentXml(
                                    batchDtos,
                                    existingMaster,
                                    batchSales
                                );

                            var addResponse =
                                await syspro.Transaction(
                                    sessionId,
                                    "SORTOI",
                                    sortoiParameter,
                                    sortoiDocument
                                );

                            string addResult = addResponse.Body.PostResult;

                            await SaveSysproLog(
                                salesOrder,
                                $"SORTOI_BATCH_{(i / 10) + 1}",
                                sortoiDocument,
                                addResult
                            );

                            if (addResult.Contains("<ErrorDescription>"))
                            {
                                existingMaster.OrderStatus = "4";

                                await _sysContext.SaveChangesAsync();

                                return BadRequest(new
                                {
                                    message = $"SORTOI Batch {(i / 10) + 1} Failed",
                                    salesOrder,
                                    response = addResult
                                });
                            }
                        }
                    }

                    // ============================================
                    // STEP 3
                    // UPDATE MBOMFLAG
                    // ============================================
                    var updateDetails = await _sysContext.SorDetails
                        .Where(c =>
                            c.SalesOrder == salesOrder &&
                            c.MbomFlag != "P")
                        .ToListAsync();
                    var deliveryDetails = await _context.DeliveryOrderDetails
                        .Where(c => c.SalesOrder == salesOrder)
                        .ToListAsync();

                    var stockCodes = updateDetails
     .Select(x => x.MstockCode)
     .Distinct()
     .ToList();

                    var invMultBins = await _sysContext.InvMultBins
                        .Where(x => stockCodes.Contains(x.StockCode))
                        .ToListAsync();
                    
                    foreach (var detail in updateDetails)
                    {

                        if (detail.MbackOrderQty > 0)
                        {
                            var set = deliveryDetails.Where(c => c.MstockCode==detail.MstockCode).Select(x => x.Set).Distinct().ToList().FirstOrDefault();
                            var bin = invMultBins.Where(c => c.StockCode == detail.MstockCode && c.Bin == set).OrderByDescending(t => t.QtyOnHand1).FirstOrDefault();
                            if (bin != null)
                            {
                                var availableBin = bin.Bin;
                                if (availableBin != null)
                                {
                                    bin.SoQtyToShip = 0;
                                    
                                }
                            }
                        }
                    }
                    await _sysContext.SaveChangesAsync();
                    _sysContext.ChangeTracker.Clear();


                    foreach (var detail in updateDetails)
                    {
                        if (detail.MbackOrderQty > 0)
                        {
                            var set = deliveryDetails.Where(c => c.MstockCode == detail.MstockCode).Select(x => x.Set).Distinct().ToList().FirstOrDefault();

                            var bin = invMultBins.Where(c => c.QtyOnHand1 >= detail.MbackOrderQty && c.Bin == set && c.StockCode == detail.MstockCode).OrderByDescending(t => t.QtyOnHand1).FirstOrDefault();
                            if (bin != null)
                            {
                                var availableBin = bin.Bin;
                                if (availableBin != null)
                                {
                                    string sortboParameter =
                        SortBoParamxml();

                                    string sortBoxmlIn =
                                        SortBoXml(availableBin,
                                            detail
                                        );

                                    var sortBoResponse =
                                        await syspro.Transaction(
                                            sessionId,
                                            "SORTBO",
                                            sortboParameter,
                                            sortBoxmlIn
                                        );
                                    await SaveSysproLog(
                                    salesOrder,
                                    "SORTBO",
                                    sortBoxmlIn,
                                    sortBoResponse.Body.PostResult
                                );
                                }
                            }
                        }
                    }
                    _sysContext.ChangeTracker.Clear();
                    var updateDetails1 = await _sysContext.SorDetails
                        .Where(c =>
                            c.SalesOrder == salesOrder &&
                            c.MbomFlag != "P")
                        .ToListAsync();

                    foreach (var detail in updateDetails1)
                    {
                        detail.MbomFlag = "C";
                        detail.MparentKitType = "K";
                        detail.MqtyChangesFlag = "Y";
                    }
                    await _sysContext.SaveChangesAsync();
                    var CurrentDate = DateTime.Now;
                    foreach (var dto in deliveryDetails )
                    {
                       
                        
                            dto.Status = "PostedToSyspro";
                            dto.PostedDate = CurrentDate;
                    }
                }

                // ============================================
                // SAVE DATABASE
                // ============================================
                await _context.SaveChangesAsync();

                // ============================================
                // LOGOUT
                // ============================================
                await syspro.LogoutAsync(sessionId);
                string allErrors = "";
                if (warnMessages.Length > 0)
                {
                    allErrors = warnMessages.ToString();
                    // Log, return, or throw exception
                }
                return Ok(new
                {
                    message = "Successfully posted to SYSPRO "

                });
            }
            catch (Exception ex)
            {
                await SaveSysproLog(
                               items.FirstOrDefault()?.SalesOrder,
                               $"Exception",
                               ex.Message.ToString(),
                               ex.StackTrace.ToString()
                           );
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("sysprologs")]
        public async Task<IActionResult> GetSysproLogs()
        {
            var logs = await _context.SysproPostLogs
                .OrderByDescending(x => x.CreatedTime)
                .Take(1000)
                .ToListAsync();

            return Ok(logs);
        }
        private async Task SaveSysproLog(
    string salesOrder,
    string businessObject,
    string xmlIn,
    string xmlOut)
        {
            _context.SysproPostLogs.Add(new SysproPostLog
            {
                SalesOrder = salesOrder,
                Event = businessObject,
                Action = xmlIn,
                EventMessage = xmlOut,
                CreatedBy = "SYSTEM",
                CreatedTime = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
        private string BuildSortoxParameterXml()
        {
            StringBuilder sb = new StringBuilder();

           
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""Windows-1252""?>");

            sb.AppendLine(
                @"<PostSalesOrderCancel xmlns:xsd=""http://www.w3.org/2001/XMLSchema-instance"" xsd:noNamespaceSchemaLocation=""SORTOX.XSD"">");
            sb.AppendLine(@"  <Parameters>");
            sb.AppendLine(@"      <ValidateOnly>N</ValidateOnly>");
            sb.AppendLine(@"  </Parameters>");

            sb.AppendLine(@"</PostSalesOrderCancel>");
            Console.WriteLine(sb.ToString());
            return sb.ToString();
        }
        private string BuildSortoxDocumentXml(
    string salesOrder,
    List<SorDetail> lines,
    string cancelReasonCode)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(
                @"<?xml version=""1.0"" encoding=""Windows-1252""?>");

            sb.AppendLine(@"
<PostSalesOrderCancel xmlns:xsd=""http://www.w3.org/2001/XMLSchema-instance"" xsd:noNamespaceSchemaLocation=""SORTOXDOC.XSD"">");

            foreach (var line in lines.Where(c => c.MbomFlag != "P"))
            {
                sb.AppendLine(@"  <Item>");
                 
                sb.AppendLine(
                    $@"      <SalesOrder>{salesOrder}</SalesOrder>");

                sb.AppendLine(
                    $@"      <SalesOrderLine>{line.SalesOrderLine}</SalesOrderLine>");

                sb.AppendLine(
                    @"      <MarkLineComplete>N</MarkLineComplete>");

                sb.AppendLine(
                    $@"      <CancelReasonCode>02</CancelReasonCode>");

                sb.AppendLine(@"      <eSignature/>");

                sb.AppendLine(@"  </Item>");
            }

            sb.AppendLine(@"</PostSalesOrderCancel>");
            Console.WriteLine(sb.ToString());
            return sb.ToString();
        }
        private string BuildSortoiParameterXml()
        {
            StringBuilder Parameter = new StringBuilder();

            Parameter.AppendLine(
                @"<?xml version=""1.0"" encoding=""Windows-1252""?>");

            Parameter.AppendLine(@"
<SalesOrders xmlns:xsd=""http://www.w3.org/2001/XMLSchema-instance"" xsd:noNamespaceSchemaLocation=""SORTOI.XSD"">");

            Parameter.AppendLine(@"  <Parameters>");

            Parameter.AppendLine(@"      <InBoxMsgReqd>Y</InBoxMsgReqd>");
            Parameter.AppendLine(@"      <Process>IMPORT</Process>");
            Parameter.AppendLine(@"      <TypeOfOrder>ORD</TypeOfOrder>");
            Parameter.AppendLine(@"      <OrderStatus>1</OrderStatus>");
            Parameter.AppendLine(@"      <AllowNonStockItems>Y</AllowNonStockItems>");
            Parameter.AppendLine(@"      <AcceptOrdersIfNoCredit>Y</AcceptOrdersIfNoCredit>");
            Parameter.AppendLine(@"      <IgnoreWarnings>Y</IgnoreWarnings>");
            Parameter.AppendLine(@"      <AlwaysUsePriceEntered>Y</AlwaysUsePriceEntered>");
            Parameter.AppendLine(@"      <AllowZeroPrice>Y</AllowZeroPrice>");
            Parameter.AppendLine(@"      <AllowChangeToZeroPrice>Y</AllowChangeToZeroPrice>");

            Parameter.AppendLine(@"  </Parameters>");

            Parameter.AppendLine(@"</SalesOrders>");
            Console.WriteLine(Parameter.ToString());
            return Parameter.ToString();
        }
        private string SortBoParamxml()
        {
            //Declaration
            StringBuilder Parameter = new StringBuilder();

            //Building Parameter content
            Parameter.Append("<?xml version=\"1.0\" encoding=\"Windows-1252\"?>");
            Parameter.Append("<!-- Copyright 1994-2010 SYSPRO Ltd.-->");
            Parameter.Append("<!--");
            Parameter.Append("This is an example XML instance to demonstrate");
            Parameter.Append("use of the Sales Order Back Order Release Business Object");
            Parameter.Append("-->");
            Parameter.Append("<PostSorBackOrderRelease xmlns:xsd=\"http://www.w3.org/2001/XMLSchema-instance\" xsd:noNamespaceSchemaLocation=\"SORTBO.XSD\">");
            Parameter.Append("<Parameters>");
            Parameter.Append("<IgnoreWarnings>N</IgnoreWarnings>");
            Parameter.Append("<ApplyIfEntireDocumentValid>Y</ApplyIfEntireDocumentValid>");
            Parameter.Append("<ValidateOnly>N</ValidateOnly>");
            Parameter.Append("<AddQuantityToBatchSerial>N</AddQuantityToBatchSerial>");
            Parameter.Append("<IgnoreAutoDepletion>N</IgnoreAutoDepletion>");
            Parameter.Append("<ShipKitFromDefaultBin>N</ShipKitFromDefaultBin>");
            Parameter.Append("<PickFunction>A</PickFunction>");
            Parameter.Append("<DestinationBin></DestinationBin>");
            Parameter.Append("	<DestinationWarehouse>FG</DestinationWarehouse>");
            Parameter.Append("<CreateMission>N</CreateMission>");
            Parameter.Append("	<Picker></Picker>");
            Parameter.Append("<Pick></Pick>");
            Parameter.Append("<PickSequence>B</PickSequence>");
            Parameter.Append("</Parameters>");
            Parameter.Append("</PostSorBackOrderRelease>");
            return Parameter.ToString() ;
        }
        private string SortBoXml(string bin, SorDetail sorDetail)
        {
            //Declaration
            StringBuilder Document = new StringBuilder();

            //Building Document content
            Document.Append("<?xml version=\"1.0\" encoding=\"Windows-1252\"?>");
            Document.Append("<!-- Copyright 1994-2010 SYSPRO Ltd.-->");
            Document.Append("<!--");
            Document.Append("This is an example XML instance to demonstrate");
            Document.Append("use of the Sales Order Back Order Release Business Object");
            Document.Append("-->");
            Document.Append("<PostSorBackOrderRelease xmlns:xsd=\"http://www.w3.org/2001/XMLSchema-instance\" xsd:noNamespaceSchemaLocation=\"SORTBODOC.XSD\">");
            Document.Append("<Item>");
            Document.Append("");
            Document.Append($@"<SalesOrder>{sorDetail.SalesOrder}</SalesOrder>");
            Document.Append($@"<StockCode>{sorDetail.MstockCode}</StockCode>");
            Document.Append($@"<Warehouse>{sorDetail.Mwarehouse}</Warehouse>");
            Document.Append($@"<Quantity>{sorDetail.MbackOrderQty}</Quantity>");
            Document.Append("<ReleaseFromMultipleLines>N</ReleaseFromMultipleLines>");
            Document.Append($@"<SalesOrderLine>{sorDetail.SalesOrderLine}</SalesOrderLine>");
            Document.Append("<Bins>");
            Document.Append($@"<BinLocation>{bin}</BinLocation>");
            Document.Append($@"<BinQuantity>{sorDetail.MbackOrderQty}</BinQuantity>");
            Document.Append("<BinUnits/>");
            Document.Append("<BinPieces/>");
            Document.Append("</Bins>");
            Document.Append("<OrderStatus>N</OrderStatus>");
            Document.Append("<ReleaseFromShip>N</ReleaseFromShip>");
            Document.Append("</Item>");
            Document.Append("</PostSorBackOrderRelease>");
            return Document.ToString();
        }
        private string BuildSortoiDocumentXml(
    List<DeliveryOrderDetailDto> items,
    SorMaster master,
    List<SorDetail> existingOrderDetails)
        {
            StringBuilder Document = new StringBuilder();

            Document.AppendLine(
                @"<?xml version=""1.0"" encoding=""Windows-1252""?>");

            Document.AppendLine(@"
<SalesOrders xmlns:xsd=""http://www.w3.org/2001/XMLSchema-instance"" xsd:noNamespaceSchemaLocation=""SORTOIDOC.XSD"">");

            // ============================================
            // TRANSMISSION HEADER
            // ============================================
            Document.AppendLine(@"  <TransmissionHeader>");
            Document.AppendLine(
                @"      <TransmissionReference>1</TransmissionReference>");
            Document.AppendLine(@"      <SenderCode/>");
            Document.AppendLine(@"  </TransmissionHeader>");

            // ============================================
            // ORDERS
            // ============================================
            Document.AppendLine(@"  <Orders>");

            // ============================================
            // ORDER HEADER
            // ============================================
            Document.AppendLine(@"      <OrderHeader>");

            Document.AppendLine(
                $@"          <CustomerPoNumber>{master.CustomerPoNumber}</CustomerPoNumber>");

            Document.AppendLine(
                @"          <OrderActionType>C</OrderActionType>");

            Document.AppendLine(
                $@"          <Customer>{master.Customer}</Customer>");

            Document.AppendLine(
                $@"          <SalesOrder>{master.SalesOrder}</SalesOrder>");

            Document.AppendLine(@"      </OrderHeader>");

            // ============================================
            // ORDER DETAILS
            // ============================================
            Document.AppendLine(@"      <OrderDetails>");

            foreach (var item in items.Where(c => c.MbomFlag != "P" && c.Usage>0 ))
            {
                // ============================================
                // GET EXISTING SO DETAIL
                // ============================================
                var existingDetail = existingOrderDetails
                    .FirstOrDefault(x =>
                        x.SalesOrder == item.SalesOrder &&
                        x.SalesOrderLine == item.SalesOrderLine );

                // DEFAULT UOM
                string orderUom = "EA";
                string priceUom = "EA";
                string Stockdesc = "";
                string price = "0";

                string priceCode = "";
                

                if (existingDetail != null)
                {
                    orderUom = existingDetail.MorderUom ?? "EA";
                    priceUom = existingDetail.MpriceUom ?? "EA";
                    Stockdesc = existingDetail.MstockDes;
                    price = existingDetail.Mprice.ToString();

                }

                Document.AppendLine(@"          <StockLine>");

                Document.AppendLine(
                    $@"              <CustomerPoLine>
                {item.SalesOrderLine}
               </CustomerPoLine>");

                Document.AppendLine(
                    @"              <LineActionType>A</LineActionType>");

                Document.AppendLine(
                    $@"              <StockCode>{item.MstockCode}</StockCode>");
                Document.AppendLine(
                   $@"              <StockDescription>{Stockdesc}</StockDescription>");
                Document.AppendLine(
                    $@"              <Warehouse>{item.Mwarehouse}</Warehouse>");

                Document.AppendLine(
                    $@"              <OrderQty>{item.Usage ?? 0}</OrderQty>");

                // ============================================
                // UOM FROM SORDETAIL
                // ============================================
                Document.AppendLine(
                    $@"              <OrderUom>{orderUom}</OrderUom>");

                Document.AppendLine(
                    $@"              <PriceUom>{priceUom}</PriceUom>");
                Document.AppendLine(
                    $@"              <Price>{price}</Price>");
                //if (priceCode != "!")
                //{
                //    Document.AppendLine(
                //        $@"              <PriceCode>{priceCode}</PriceCode>");
                //}

                Document.AppendLine(@"          </StockLine>");
            }

            Document.AppendLine(@"      </OrderDetails>");

            Document.AppendLine(@"  </Orders>");

            Document.AppendLine(@"</SalesOrders>");
            Console.WriteLine(Document.ToString());
            return Document.ToString();
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
        [Authorize]
        public async Task<IActionResult> SaveDeliveryOrders([FromBody] DeliveryOrderDetailPayload payload)
        {
            var items = payload.Data;
            if (items == null || !items.Any())
                return BadRequest("No data received.");
            if (items.GroupBy(x => new { x.SalesOrder, x.SalesOrderLine }).Any(group => group.Count() > 1))
                return BadRequest("Duplicate sales order lines were submitted.");
            // Validate the complete capture against source lines, not client quantities/kit flags.
            if (payload.Procedures != null)
            {
                if (items.Select(x => x.SalesOrder).Distinct().Count() != 1)
                    return BadRequest("Capture procedures for one sales order at a time.");
                var salesOrder = items[0].SalesOrder.Trim();
                foreach (var item in items) item.SalesOrder = salesOrder;
                var source = await _sysContext.VwFetchSordetails
                    .Where(x => x.SalesOrder == salesOrder && x.OrderStatus == "4")
                    .OrderBy(x => x.SalesOrderLine).ToListAsync();
                if (source.Count == 0)
                    return BadRequest("This sales order is no longer in warehouse status. No data was saved.");
                var sourceLines = source.Select(x => (int)x.SalesOrderLine).ToHashSet();
                var submittedLines = items.Select(x => x.SalesOrderLine).ToHashSet();
                if (source.Count != sourceLines.Count)
                    return BadRequest("The source order contains duplicate line numbers. Please contact support; no data was saved.");
                if (!sourceLines.SetEquals(submittedLines))
                {
                    var missing = string.Join(", ", sourceLines.Except(submittedLines).OrderBy(x => x));
                    var removed = string.Join(", ", submittedLines.Except(sourceLines).OrderBy(x => x));
                    return BadRequest($"The capture does not match the complete order. Missing from capture: {missing}. No longer in order: {removed}. Reopen the order to load all lines. No data was saved.");
                }
                var procedures = payload.Procedures.OrderBy(x => x.ProcedureNumber).ToList();
                if (procedures.Count == 0 || !procedures.Select(x => x.ProcedureNumber).SequenceEqual(Enumerable.Range(1, procedures.Count)) ||
                    procedures.Any(x => x.ProcedureDate == default || x.LineNumbers == null || x.LineNumbers.Count == 0 ||
                        string.IsNullOrWhiteSpace(x.PatientNumber) || string.IsNullOrWhiteSpace(x.PatientName) ||
                        string.IsNullOrWhiteSpace(x.SurgeonName) || string.IsNullOrWhiteSpace(x.CgSetNumber)))
                    return BadRequest("Complete all patient details and assign kits to each sequential procedure.");
                var assigned = procedures.SelectMany(x => x.LineNumbers).ToList();
                if (assigned.Count != items.Count || assigned.Distinct().Count() != assigned.Count ||
                    !assigned.ToHashSet().SetEquals(items.Select(x => x.SalesOrderLine)))
                    return BadRequest("Assign every order line to exactly one procedure.");
                foreach (var line in source)
                {
                    var item = items.Single(x => x.SalesOrderLine == (int)line.SalesOrderLine);
                    if (line.MbomFlag != "P" && (!item.RepUsageQty.HasValue || item.RepUsageQty < 0 || item.RepUsageQty > line.MshipQty))
                        return BadRequest("Usage must be between zero and the delivered quantity.");
                }
                var previous = await _context.PatientProcedures.Where(x => x.SalesOrder == salesOrder).ToListAsync();
                // Update matching records in place so a retry cannot introduce duplicate keys.
                _context.PatientProcedures.RemoveRange(previous.Where(x => x.ProcedureNumber > procedures.Count));
                foreach (var procedure in procedures)
                {
                    var entity = previous.SingleOrDefault(x => x.ProcedureNumber == procedure.ProcedureNumber);
                    if (entity == null)
                    {
                        entity = new PatientProcedure { SalesOrder = salesOrder, ProcedureNumber = procedure.ProcedureNumber };
                        _context.PatientProcedures.Add(entity);
                    }
                    entity.PatientNumber = procedure.PatientNumber.Trim();
                    entity.PatientName = procedure.PatientName.Trim();
                    entity.SurgeonName = procedure.SurgeonName.Trim();
                    entity.ProcedureDate = procedure.ProcedureDate;
                    entity.CgSetNumber = procedure.CgSetNumber.Trim();
                    entity.LineNumbersJson = System.Text.Json.JsonSerializer.Serialize(procedure.LineNumbers);
                }
            }
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var routedClerk = await _context.Users.Where(x => x.Username == username).Select(x => x.DefaultRouteClerk).FirstOrDefaultAsync();
            if (routedClerk == null)
                return BadRequest("No default route clerk set for the user.Please reach out admin team.");

            // Group by SalesOrder
            var groupedBySalesOrder = items
                .GroupBy(dto => dto.SalesOrder);

            var insertedCount = 0;
            var updatedCount = 0;
            await using var transaction = await _context.Database.BeginTransactionAsync();
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
                        updatedCount++;
                        existing.RepVerNumber = finalNumber;
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
                                RepUsageQty = dto.RepUsageQty ?? 0,
                                RepEntertedDate = DateTime.Now,
                                RepName = username,
                                RepVerNumber= finalNumber,
                                RoutedClerk = routedClerk,
                            };

                            _context.DeliveryOrderDetails.Add(newEntity);
                            insertedCount++;
                       
                    }
                }

               
            }

            

            // Persist procedure headers and every order line in the same transaction.
            await _context.SaveChangesAsync();
            foreach (var group in groupedBySalesOrder)
            {
                var expectedLines = group.Select(x => x.SalesOrderLine).ToHashSet();
                var savedLines = await _context.DeliveryOrderDetails.AsNoTracking()
                    .Where(x => x.SalesOrder == group.Key && x.RepVerNumber == finalNumber)
                    .Select(x => x.Line).ToListAsync();
                if (!expectedLines.IsSubsetOf(savedLines))
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "The save could not verify every submitted order line. No changes were committed. Please retry.");
                }
            }
            await transaction.CommitAsync();
            return Ok(new { message = "Saved successfully", count = insertedCount + updatedCount, insertedCount, updatedCount, repVerNumber = finalNumber });

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

        [HttpPost("AdminUpdateOrderStatus")]
        public async Task<IActionResult> AdminUpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SalesOrderNumber))
                    return BadRequest("Sales Order Number is required.");

                if (string.IsNullOrEmpty(request.CorrectionType))
                    return BadRequest("Correction Type is required.");

                var username = User.FindFirst(ClaimTypes.Name)?.Value;

                // Format the sales order number to 15 digits with leading zeros
                var salesOrderNumber = request.SalesOrderNumber;
                if (long.TryParse(salesOrderNumber, out long numericOrder))
                {
                    salesOrderNumber = numericOrder.ToString("D15");
                }

                // Get the existing order details
                var existingOrders = await _context.DeliveryOrderDetails
                    .Where(x => x.SalesOrder == salesOrderNumber)
                    .ToListAsync();

                if (!existingOrders.Any())
                {
                    

                }
                else
                {
                    // Update existing orders
                    string newStatus = request.CorrectionType == "repclerk" ? "StoresInProgress" : "Inprogress";


                    foreach (var order in existingOrders)
                    {
                        order.Status = newStatus;
                    }
                }

                await _context.SaveChangesAsync();

                string routeType = request.CorrectionType == "repclerk" ? "Rep Clerk" : "Rep";
                return Ok(new
                {
                    message = $"Order {request.SalesOrderNumber} has been updated",
                    salesOrder = request.SalesOrderNumber,
                    correctionType = request.CorrectionType,
                    routedTo = ""
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("GetOrderStatusHistory")]
        public async Task<IActionResult> GetOrderStatusHistory([FromQuery] string salesOrderNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(salesOrderNumber))
                    return BadRequest("Sales Order Number is required.");

                // Format the sales order number
                if (long.TryParse(salesOrderNumber, out long numericOrder))
                {
                    salesOrderNumber = numericOrder.ToString("D15");
                }

                var orders = await _context.DeliveryOrderDetails
                    .Where(x => x.SalesOrder == salesOrderNumber)
                    .OrderByDescending(x => x.RepEntertedDate)
                    .ThenByDescending(x => x.ClerkDate)
                    .ToListAsync();

                if (!orders.Any())
                    return NotFound("No status history found for this order.");

                var history = orders.Select(o => new
                {
                    o.SalesOrder,
                    o.Line,
                    o.Status,
                    o.RepName,
                    o.RepEntertedDate,
                    o.ClerkName,
                    o.ClerkDate,
                    o.AdminVerNumber,
                    o.RoutedClerk,
                    o.PostedBy,
                    o.PostedDate
                });

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    inner = ex.InnerException?.Message
                });
            }
        
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

        [Authorize]
        [HttpPost("patientproceduredetails")]
        public async Task<IActionResult> GetPatientProcedureDetails([FromBody] SalesOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SalesOrderNumber))
                return BadRequest("A sales order number is required.");
            var salesOrder = request.SalesOrderNumber.Trim();
            if (long.TryParse(salesOrder, out var numericOrder)) salesOrder = numericOrder.ToString("D15");
            var procedures = await _context.PatientProcedures.AsNoTracking()
                .Where(p => p.SalesOrder == salesOrder).OrderBy(p => p.ProcedureNumber).ToListAsync();
            var lines = await _context.DeliveryOrderDetails.AsNoTracking()
                .Where(line => line.SalesOrder == salesOrder).ToListAsync();
            var byLine = lines.ToDictionary(line => line.Line);
            var result = new List<object>();
            foreach (var procedure in procedures)
            {
                var numbers = System.Text.Json.JsonSerializer.Deserialize<List<int>>(procedure.LineNumbersJson) ?? new List<int>();
                result.Add(new {
                    procedure.SalesOrder, procedure.ProcedureNumber, procedure.PatientNumber,
                    procedure.PatientName, procedure.SurgeonName, procedure.ProcedureDate, procedure.CgSetNumber,
                    Lines = numbers.OrderBy(n => n).Select(n => {
                        byLine.TryGetValue(n, out var line);
                        return new {
                            SalesOrderLine = n, MissingSavedLine = line == null,
                            SetsCode = line?.Set, line?.MstockCode, line?.MstockDes, line?.Mwarehouse,
                            line?.MorderQty, line?.MshipQty, line?.RepUsageQty,
                            line?.RetQty, line?.Usage, line?.Variance
                        };
                    }).ToList()
                });
            }
            return Ok(result);
        }

        [HttpPost("clerkorderdetails")]
        public async Task<IActionResult> GetRepClerkSalesOrders([FromBody] SalesOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SalesOrderNumber))
                return BadRequest("A sales order number is required.");
            request.SalesOrderNumber = request.SalesOrderNumber.Trim();
            if (long.TryParse(request.SalesOrderNumber, out long numericOrder))
            {
                // Format to 15 digits with leading zeros
                request.SalesOrderNumber = numericOrder.ToString("D15");
            }


            var order = await _sysContext.VwFetchSordetails.AsNoTracking().Where(o => o.OrderStatus == "4" && o.SalesOrder == request.SalesOrderNumber)
                .ToListAsync();
            if (!String.IsNullOrEmpty(request?.SalesOrderNumber))
                order = order
                    .Where(o => o.SalesOrder == request.SalesOrderNumber)
                    .ToList();

            if (order?.Count == 0)
                return NotFound("Sales order not found.");

            if (!order.Any(o => o.Status == "RepCompleted" || o.Status == "Send Email To Customer Service" || o.Status == "StoresInProgress" || o.Status == "Completed&ReadyForValidation" || o.Status == "ReadyToPostSyspro"))
                return BadRequest("This order is not ready for RepClerk review.");
            return Ok(order);
        }
        [HttpPost("saleorderdetails")]
        public async Task<IActionResult> GetRepSalesOrders([FromBody] SalesOrderRequest request)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(request.SalesOrderNumber))
                    return BadRequest("A sales order number is required.");
                request.SalesOrderNumber = request.SalesOrderNumber.Trim();
                if (long.TryParse(request.SalesOrderNumber, out long numericOrder))
                {
                    // Format to 15 digits with leading zeros
                    request.SalesOrderNumber = numericOrder.ToString("D15");
                }
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var order = await _sysContext.VwFetchSordetails.AsNoTracking().Where(o => o.OrderStatus == "4" && o.SalesOrder == request.SalesOrderNumber)
                    .ToListAsync();
                if (!String.IsNullOrEmpty(request?.SalesOrderNumber))
                    order = order
                        .Where(o => o.SalesOrder == request.SalesOrderNumber)
                        .ToList();
                var roles = User.FindFirst(ClaimTypes.Role)?.Value;
                var salesPerson = User.FindFirst(ClaimTypes.GivenName)?.Value;
                if (roles == "rep")
                {
                    // Eligibility is order-wide. Never hide individual kit lines:
                    // final save validates and persists the entire source order.
                    if (!order.Any(o => o.Status == null || o.Status == "Inprogress"))
                        return BadRequest("This order is no longer available for Rep capture.");

                }
                if (roles == "repclerk")
                {
                    order = order.Where(o => o.Status == "RepCompleted" || o.Status == "StoresInProgress" || o.Status == "Completed&ReadyForValidation" || o.Status == "Send Email To Customer Service" || o.Status == "ReadyToPostSyspro").ToList();

                }
                if (order?.Count == 0)
                    return NotFound("Sales order not found.");
                return Ok(order);
            }
            catch (Exception ex)
            {
                // 🔥 TEMP: return full error
                return StatusCode(500, new
                {
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    inner = ex.InnerException?.Message
                });
            }
           
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
