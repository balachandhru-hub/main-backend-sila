using Contracts.IRepository;
using Identity.Domain.Entities;
using MediatR;
using HashingSystem;
using Microsoft.Extensions.Configuration;
using Identity.Application.Features.Commands.Login;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Domain.Dto;
using SharedKernel.ExceptionHandler;
using Microsoft.AspNetCore.Http;
using Identity.Domain.Common;
using SharedKernel.LoggerServices;


namespace Identity.Application.Features.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBcryptHashing _hashing;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILoggerManager _logger;



        public LoginCommandHandler(
            IRepositoryWrapper repository,
            IBcryptHashing hashing,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILoggerManager logger
           )
        {
            _repository = repository;
            _hashing = hashing;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

        }



        public async Task<LoginResponse> Handle(
           LoginCommand request,
           CancellationToken cancellationToken)
        {
            int maxAttempts = int.TryParse(_configuration["LoginSecurity:MaxFailedAttempts"], out int m) ? m : 5;
            int lockoutMinutes = int.TryParse(_configuration["LoginSecurity:LockoutMinutes"], out int l) ? l : 2;
            int maxActiveSessions = int.TryParse(_configuration["TokenSecurity:MaxActiveSessions"], out int s) ? s : 3;

            _logger.LogInfo($"Authentication request received for user: {request.UserName}");

            string ipAddress = _httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Real-IP"].FirstOrDefault()
                ?? _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.MapToIPv4()?.ToString();

            User? user = _repository.User
                .FindFirstByCondition(u => u.UserName == request.UserName);

            if (user == null)
            {
                _logger.LogError($"Invalid username or password for user: {request.UserName}");
                throw new UnAuthorizedCustomException(
                    "Invalid username or password",
                    "Invalid username or password.");
            }
            else if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                throw new TooManyRequestsCustomException(
                    "Maximum limit reached. Please try again later.",
                    $"Try again after {user.LockoutEnd} UTC");
            }

            else if (!_hashing.VerifyHash(request.Password, user.UserSecret))
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= maxAttempts)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                }

                user.LastFailedLogin = DateTime.UtcNow;
                _repository.User.Update(user);

                _repository.Save();

                _logger.LogError($"Invalid password for user: {request.UserName}");

                if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
                {
                    throw new TooManyRequestsCustomException(
                        "Maximum limit reached",
                        $"Try again after {user.LockoutEnd}");
                }

                throw new UnAuthorizedCustomException(
                    "Invalid username or password",
                    "Invalid username or password");
            }


            else
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                _repository.User.Update(user);

                UserRoleMapping? userRoleMapping = _repository.UserRoleMapping
                    .FindByCondition(rm => rm.UserId == user.Id).FirstOrDefault();

                if (userRoleMapping == null)
                {
                    _logger.LogError($"UserRoleMapping not found for userId: {user.Id}");
                    throw new UnAuthorizedCustomException("Unauthorized", "User role not found.");
                }

                Person? person = _repository.Person
                    .FindFirstByCondition(p => p.IsActive && p.Id == user.PersonId);

                if (person == null)
                {
                    _logger.LogError($"Person Not Found for userId : {user.Id}");
                    throw new UnAuthorizedCustomException("Unauthorized", "Person Not Found.");
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Role, userRoleMapping.RoleId.ToString()),
                    new Claim("PersonId", user.PersonId.ToString()),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("OrganizationId", person.OrganizationId.ToString())
                };

                int number = int.TryParse(_configuration[Common.TOKEN_EXPIRY], out int result)
                    ? result : Common.TOKEN_EXPIRY_TIME_DEFAULT;

                DateTime expirationTime = DateTime.UtcNow.AddSeconds(number);
                string jwtToken = GenerateToken(claims, expirationTime);

                var activeTokens = _repository.AccessToken
                   .FindByCondition(x => x.UserId == user.Id && x.IsActive)
                   .OrderByDescending(x => x.ExpiredTime)
                   .ToList();

                if (activeTokens.Count > maxActiveSessions)
                {
                    var tokensToDeactivate = activeTokens.Skip(maxActiveSessions).ToList();

                    foreach (var token in tokensToDeactivate)
                    {
                        token.IsActive = false;

                    }
                    _repository.AccessToken.UpdateRange(tokensToDeactivate);
                }
                AccessToken accessToken = new AccessToken
                {
                    Id = Guid.NewGuid(),
                    AccessTokenValue = jwtToken,
                    ExpiredTime = expirationTime,
                    UserId = user.Id,
                    RefreshToken = Guid.NewGuid(),
                };

                _repository.AccessToken.Create(accessToken);


                LoginRecord loginRecord = new LoginRecord
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    OrganizationId = person.OrganizationId,
                    LoginTime = DateTime.UtcNow,
                    EntityType = Common.LOGIN_ATTRIBUTE_LOGIN,
                    IpAddress = ipAddress,
                    AttributeId = user.Id.ToString()
                };

                _repository.LoginRecord.Create(loginRecord);
                RefreshToken refreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = accessToken.RefreshToken.ToString(),
                    ExpiresOn = expirationTime.AddDays(7),
                    
                };
 
                _repository.RefreshToken.Create(refreshToken);
                _repository.Save();

                _logger.LogInfo($"Access token created for user: {user.Id}");

                return new LoginResponse
                {
                    Token = jwtToken,
                    RefreshToken = accessToken.RefreshToken
                };
            }
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
}