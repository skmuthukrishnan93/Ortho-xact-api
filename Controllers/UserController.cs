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
using System.Text;

namespace Ortho_xact_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {



        private readonly IConfiguration _configuration;
        private readonly SysproContext _sysContext;
        private readonly OrthoxactContext _context;

        public UserController(OrthoxactContext context, IConfiguration configuration,SysproContext sysproContext)
        {
            _context = context;
            _configuration = configuration;
            _sysContext = sysproContext;
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequests request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest( new { success = false, message = "Username and password are required." });
            }

            var user = _context.Users
                .FirstOrDefault(u => u.Username == request.Username && u.Password == request.Password);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid username or password."
                });
            }

            // Optional: exclude password from response
            user.Password = null;
            var token = GenerateJwtToken(user);
            return Ok(new
            {
                success = true,
                message = "Login successful.",
                token = token,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Roles
                }
            });
        }
        [Authorize]
        [HttpPost("createuser")]
        public IActionResult CreateUser([FromBody] CreateUserRequest request)
        {
            if (request == null)
                return BadRequest(new
                {
                    success = false,
                    message = "User data is required."
                });
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            // Basic validation (you can extend it)
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new
                {
                    success = false,
                    message = "Username and password are required."
                });

            // Check if username already exists
            var existingUser = _context.Users.FirstOrDefault(u => u.Username == request.Username);
            if (existingUser != null)
                return Conflict(new { success = false, message= "Username already exists." });

            var newUser = new User
            {
                Username = request.Username,
                Password = request.Password, // Ideally hash password before saving!
                Firstname = request.Firstname,
                Lastname = request.Lastname,
                Email = request.Email,
                Roles = request.Roles,
                CreatedBy = username,
                CreatedDate = DateTime.Now,
                Salesperson = request.Salesperson,
                CustomerEmail=request.Customeremail,
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return Ok();
        }
        [Authorize]
        [HttpPost("updateuser")]
        public IActionResult UpdateUser([FromBody] CreateUserRequest request)
        {
            if (request == null)
                return BadRequest(new
                {
                    success = false,
                    message = "User data is required."
                });
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            // Basic validation (you can extend it)
           
            // Check if username already exists
            var existingUser = _context.Users.FirstOrDefault(u => u.Username == request.Username);
           existingUser.Salesperson=request.Salesperson;
            existingUser.Firstname=request.Firstname??"";
            existingUser.Roles= request.Roles;
            existingUser.Email= request.Email;
            existingUser.CustomerEmail= request.Customeremail;

            
            _context.SaveChanges();

            return Ok();
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
            var exisitingOrder = _context.DeliveryOrderDetails.Where(c => c.SalesOrder==request.SalesOrderNumber).ToList();
            var fromAddress = new MailAddress("codexitza@gmail.com", "Codex IT");
            MailAddress toAddress;
             toAddress = new MailAddress("aswini@codex-it.co.za");
            const string fromPassword = "pbpufehdofruzflh";  // Not your Gmail password!
             string subject = "Salesorder#"+orderNumber+" need an action";
            //const string body = "Hello, this is a test email sent via Gmail SMTP.";
            var sb = new StringBuilder();

            sb.Append("<p>Hi Team,</p>");
            sb.Append("<p>The below order have some variance, could you please verify and confirm?</p>");

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
            sb.Append("<p>Regards,<br/>Ortho Team</p>");

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
            
            try
            {
                var message = new MailMessage();

                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;
       message.From = fromAddress;
                    if (!string.IsNullOrEmpty(toMails))
                    {
                        var toList = toMails.Split(",");
                        foreach (var to in toList)
                        {
                            message.To.Add(to);
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

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Roles ?? "User"),
         new Claim(ClaimTypes.GivenName, user.Salesperson ?? "User")
    };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
