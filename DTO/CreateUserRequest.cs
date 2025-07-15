namespace Ortho_xact_api.DTO
{
    public class CreateUserRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string Email { get; set; }
        public string Roles { get; set; }
        public string? CreatedBy { get; set; }
        public string? Salesperson { get; set; }

        public string? Customeremail { get; set; }
    }
}
