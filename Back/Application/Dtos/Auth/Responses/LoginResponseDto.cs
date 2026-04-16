namespace Back.Api.Application.Dtos;


public class LoginResponseDto : LoginClientIdentityDtoBase
{
    public string RefreshToken { get; set; } = string.Empty;
}
