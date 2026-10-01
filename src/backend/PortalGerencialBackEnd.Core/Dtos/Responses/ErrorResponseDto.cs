namespace PortalGerencialBackEnd.Core.Dtos.Responses
{
    public class ErrorResponseDto
    {
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; } = default!;
    }
}
