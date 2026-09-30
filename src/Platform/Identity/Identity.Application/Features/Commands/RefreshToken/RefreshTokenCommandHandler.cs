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
using HashingSystem;

namespace Identity.Application.Features.Commands.RefreshToken.RefreshToken;

public class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IRepositoryWrapper _repository;
    private readonly IConfiguration _configuration;
    private readonly ILoggerManager _logger;
    private readonly IBcryptHashing _hashing;

    public RefreshTokenCommandHandler(
        IRepositoryWrapper repository,
        IConfiguration configuration,
        ILoggerManager logger,
        IBcryptHashing hashing)
    {
        _repository = repository;
        _configuration = configuration;
        _logger = logger;
        _hashing = hashing;
    }

    public async Task<LoginResponse> Handle(
    RefreshTokenCommand request,
    CancellationToken cancellationToken)
{
    _logger.LogInfo("Refresh token request received.");

   
   var refreshTokens = _repository.RefreshToken
    .FindByCondition(x => x.IsActive)
    .ToList();

var refreshToken = refreshTokens.FirstOrDefault(x =>
    _hashing.VerifyHash(request.RefreshToken.ToString(), x.Token));

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

   



   var user = _repository.User
    .FindFirstByCondition(x =>
        x.Id == refreshToken.UserId &&
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

    var organization = _repository.Organization
        .FindFirstByCondition(x => x.Id == person.OrganizationId);

    if (organization == null)
    {
        throw new UnAuthorizedCustomException(
            "Unauthorized",
            "Organization not found.");
    }

    // A refreshed token has to carry the same claim set as the one issued at
    // login. SNID and OrganizationType back BaseController and MessageHub, so
    // leaving them out here broke every call made after a refresh.
    var claims = new[]
    {
        new Claim(ClaimTypes.Role, userRole.RoleId.ToString()),
        new Claim("PersonId", user.PersonId.ToString()),
        new Claim("UserId", user.Id.ToString()),
        new Claim("OrganizationId", person.OrganizationId.ToString()),
        new Claim("SNID", organization.SNID),
        new Claim("OrganizationType", organization.OrganizationType.ToString())
    };

   
   int expiry =
    int.TryParse(_configuration[Common.TOKEN_EXPIRY], out int seconds)
        ? seconds
        : Common.TOKEN_EXPIRY_TIME_DEFAULT;

    DateTime expirationTime = DateTime.UtcNow.AddSeconds(expiry);

    string jwtToken = GenerateToken(claims, expirationTime);

  
    Guid newRefreshToken = Guid.NewGuid();

  string hashedRefreshToken =
    _hashing.HashStringWithSalt(newRefreshToken.ToString());

  
   refreshToken.Token = hashedRefreshToken;
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