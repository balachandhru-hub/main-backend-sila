using Contracts.IRepository;
using Identity.Domain.Dto;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Domain.Common;

namespace Identity.Application.Features.Commands.RefreshToken.RefreshToken;

public class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IRepositoryWrapper _repository;
    private readonly IConfiguration _configuration;
    private readonly ILoggerManager _logger;

    public RefreshTokenCommandHandler(
        IRepositoryWrapper repository,
        IConfiguration configuration,
        ILoggerManager logger)
    {
        _repository = repository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(
    RefreshTokenCommand request,
    CancellationToken cancellationToken)
{
    _logger.LogInfo("Refresh token request received.");

   
    var refreshToken = _repository.RefreshToken
        .FindFirstByCondition(x =>
            x.Token == request.RefreshToken.ToString() &&
            x.IsActive);

    if (refreshToken == null)
    {
        _logger.LogError("Invalid refresh token.");

        throw new UnAuthorizedCustomException(
            "Unauthorized",
            "Invalid refresh token.");
    }

   
    if (refreshToken.ExpiresOn <= DateTime.UtcNow)
    {
        refreshToken.IsActive = false;

        _repository.RefreshToken.Update(refreshToken);
        _repository.Save();

        throw new UnAuthorizedCustomException(
            "Unauthorized",
            "Refresh token expired. Please login again.");
    }

   
var accessToken = _repository.AccessToken
    .FindFirstByCondition(x =>
        x.UserId == refreshToken.UserId &&
        x.IsActive);
    if (accessToken == null)
    {
        throw new UnAuthorizedCustomException(
            "Unauthorized",
            "Access token not found.");
    }

    
    var user = _repository.User
        .FindFirstByCondition(x =>
           x.Id == accessToken.UserId &&
            x.IsActive);

    if (user == null)
    {
        throw new UnAuthorizedCustomException(
            "Unauthorized",
            "User not found.");
    }

 
    var userRole = _repository.UserRoleMapping
        .FindFirstByCondition(x => x.UserId == user.Id);

    if (userRole == null)
    {
        throw new UnAuthorizedCustomException(
            "Unauthorized",
            "User role not found.");
    }

  
    var person = _repository.Person
        .FindFirstByCondition(x =>
            x.Id == user.PersonId &&
            x.IsActive);

    if (person == null)
    {
        throw new UnAuthorizedCustomException(
            "Unauthorized",
            "Person not found.");
    }

   
    var claims = new[]
    {
        new Claim(ClaimTypes.Role, userRole.RoleId.ToString()),
        new Claim("PersonId", user.PersonId.ToString()),
        new Claim("UserId", user.Id.ToString()),
        new Claim("OrganizationId", person.OrganizationId.ToString())
    };

   
   int expiry =
    int.TryParse(_configuration[Common.TOKEN_EXPIRY], out int seconds)
        ? seconds
        : Common.TOKEN_EXPIRY_TIME_DEFAULT;

    DateTime expirationTime = DateTime.UtcNow.AddSeconds(expiry);

    string jwtToken = GenerateToken(claims, expirationTime);

  
    Guid newRefreshToken = Guid.NewGuid();

   
    accessToken.AccessTokenValue = jwtToken;
    accessToken.ExpiredTime = expirationTime;
    accessToken.RefreshToken = newRefreshToken;

    _repository.AccessToken.Update(accessToken);

  
    refreshToken.Token = newRefreshToken.ToString();
    refreshToken.ExpiresOn = DateTime.UtcNow.AddDays(7);

    _repository.RefreshToken.Update(refreshToken);

    
    _repository.Save();

    _logger.LogInfo($"Token refreshed successfully for user {user.Id}");

    return new LoginResponse
    {
        Token = jwtToken,
        RefreshToken = newRefreshToken
    };
}
private string GenerateToken(Claim[] claims, DateTime expirationTime)
        {
            string? tokenKey = _configuration[Common.TOKEN_KEY];
            if (string.IsNullOrEmpty(tokenKey))
            {
                _logger.LogError("Token key is not configured.");
                throw new InvalidOperationException("Token key is not configured.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var issuer = _configuration[Common.TOKEN_ISSUER];
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expirationTime,
                Issuer = issuer,
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

}