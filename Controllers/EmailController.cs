using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Ortho_xact_api.DTO;
using Ortho_xact_api.Models;
using Ortho_xact_api.SysModels;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Text;

namespace Ortho_xact_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmailController : ControllerBase
    {



        private readonly IConfiguration _configuration;
        private readonly SysproContext _sysContext;
        private readonly OrthoxactContext _context;

        public EmailController(OrthoxactContext context, IConfiguration configuration,SysproContext sysproContext)
        {
            _context = context;
            _configuration = configuration;
            _sysContext = sysproContext;
        }
       
       [Authorize]
        [HttpPost("emailsettings")]
        public IActionResult EmailSettings([FromBody] EmailSetting request)
        {
            if (request == null)
                return BadRequest(new
                {
                    success = false,
                    message = "User data is required."
                });
            var username =  User.FindFirst(ClaimTypes.Name)?.Value;
            // Basic validation (you can extend it)
            request.UpdatedTime = DateTime.Now;
            request.UpdatedBy = username;
            // Check if username already exists
            var existingUser = _context.EmailSettings.FirstOrDefault();
            if (existingUser != null)
            {
             existingUser.Subject = request.Subject;
                existingUser.Body = request.Body;
                existingUser.Signature = request.Signature;
                existingUser.Cc= request.Cc;
                existingUser.Bcc= request.Bcc;
                existingUser.SmtpServer=request.SmtpServer;
                existingUser.FromAddress = request.FromAddress;
                existingUser.Password = request.Password;
                existingUser.PortNumber = request.PortNumber;
            }
            else
            {
           
                _context.EmailSettings.Add(request);
            }
            _context.SaveChanges();

            return Ok();
        }
        [Authorize]
        [HttpGet("GetEmailSettings")]
        public async Task<IActionResult> GetEmailSettings()
        {
            return Ok(_context.EmailSettings.FirstOrDefault());
        }

           [Authorize]
        [HttpPost("sendMail")]
        public async Task<IActionResult> SendEmailAsync([FromBody] SalesOrderRequest request)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var users= _context.Users.FirstOrDefault(c => c.Username == username);
            if (users.CustomerEmail == null)
            {
                return BadRequest("Customer Email Not Found");
            }
            
            string orderNumber=request.SalesOrderNumber??" ";
            if (long.TryParse(request.SalesOrderNumber, out long numericOrder))
            {
                // Format to 15 digits with leading zeros
                request.SalesOrderNumber = numericOrder.ToString("D15");
            }
            var emailsetting = _context.EmailSettings.FirstOrDefault();
            var exisitingOrder = _context.DeliveryOrderDetails.Where(c => c.SalesOrder==request.SalesOrderNumber).ToList();
            var fromAddress = new MailAddress("codexitza@gmail.com", "Codex IT");
            
            MailAddress toAddress;
            // toAddress = new MailAddress("aswini@codex-it.co.za");
             string fromPassword = "pbpufehdofruzflh";  // Not your Gmail password!
             string subject = "Salesorder#"+orderNumber+" need an action";
            //const string body = "Hello, this is a test email sent via Gmail SMTP.";
            var sb = new StringBuilder();
            if (emailsetting != null)
            {
                fromAddress = new MailAddress(emailsetting.FromAddress);
                fromPassword = emailsetting.Password;
                subject = emailsetting.Subject + " -Salesorder#" + orderNumber ;
                var body1 = emailsetting.Body.Replace("\n", "<br/>");
                sb.Append(body1);
            }
            else
            {
                sb.Append("<p>Hi Team,</p>");
                sb.Append("<p>The below order have some variance, could you please verify and confirm?</p>");
            }
            sb.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse:collapse;'>");
            sb.Append("<thead><tr style='background-color:#f2f2f2;'>");
            sb.Append("<th>S.No</th><th>Line Number</th><th>Stock Code</th><th>Order Qty</th><th>Ship Qty</th><th>Rep Qty</th><th>Clerk Qty</th><th>Usage</th><th>Variance</th>");
            sb.Append("</tr></thead><tbody>");

            int sno = 1;
            foreach (var order in exisitingOrder)
            {
                sb.Append("<tr>");
                sb.AppendFormat("<td>{0}</td>", sno++);
                sb.AppendFormat("<td>{0}</td>", order.Line);
                sb.AppendFormat("<td>{0}</td>", order.MstockCode);
                sb.AppendFormat("<td>{0}</td>", order.MorderQty);
                sb.AppendFormat("<td>{0}</td>", order.MshipQty);
                sb.AppendFormat("<td>{0}</td>", order.RepUsageQty);
                sb.AppendFormat("<td>{0}</td>", order.RetQty);
                sb.AppendFormat("<td>{0}</td>", order.Usage);
                sb.AppendFormat("<td>{0}</td>", order.Variance);
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
            sb.Append(emailsetting.Signature.Replace("\n", "<br/>"));
            //sb.Append("<p>Regards,<br/>Ortho Team</p>");

            string body = sb.ToString();
            string toMails = users?.CustomerEmail ?? string.Empty;
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword),
                Timeout = 20000,
               
            };
            if(emailsetting != null)
            {
                smtp.Port = Convert.ToInt32(emailsetting.PortNumber);
                smtp.Host = emailsetting.SmtpServer;
                
            }
            try
            {
                var message = new MailMessage();

                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;
       message.From = fromAddress;
                // toMails = "aswini@codex-it.co.za";
                toMails = users.CustomerEmail;
                    if (!string.IsNullOrEmpty(toMails))
                    {
                        var toList = toMails.Split(",");
                        foreach (var to in toList)
                        {
                            message.To.Add(to);
                        }
                    }
                    if(!string.IsNullOrEmpty (emailsetting.Bcc))
                {
                    var toList = emailsetting.Bcc.Split(",");
                    foreach (var to in toList)
                    {
                        message.Bcc.Add(to);
                    }
                }
                if (!string.IsNullOrEmpty(emailsetting.Cc))
                {
                    var toList = emailsetting.Cc.Split(",");
                    foreach (var to in toList)
                    {
                        message.CC.Add(to);
                    }
                }
                await smtp.SendMailAsync(message);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            return Ok();
        }

       

    }
}
