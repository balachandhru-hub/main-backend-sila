using Contracts.IRepository;
using Identity.Domain.Entities;
using MediatR;
using HashingSystem;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;
using Identity.Application.Features.Commands.Login;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.Application.Features.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBcryptHashing _hashing;
        private readonly IConfiguration _configuration;
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 2;

        public LoginCommandHandler(
            IRepositoryWrapper repository,
            IBcryptHashing hashing,
            IConfiguration configuration
           )
        {
            _repository = repository;
            _hashing = hashing;
            _configuration = configuration;
        
        }

        public async Task<LoginResponse> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            var user = _repository.User
                .FindByConditionAsync(x =>
                    x.UserName == request.UserName &&
                    x.IsActive)
                .FirstOrDefault();

            if (user == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password."
                };
            }

            // Account Lock
            if (user.LockoutEnd.HasValue &&
                user.LockoutEnd > DateTime.UtcNow)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Maximum limit reached. Try again after {user.LockoutEnd:yyyy-MM-dd HH:mm:ss} UTC."
                };
            }

            // Password Validation
            bool isValid =
                _hashing.VerifyHash(request.Password, user.UserSecret!);

            if (!isValid)
            {
                user.FailedLoginAttempts++;
                user.LastFailedLogin = DateTime.UtcNow;

                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockoutEnd =
                        DateTime.UtcNow.AddMinutes(LockoutMinutes);
                }

                _repository.User.Update(user);
                await _repository.SaveAsync();

                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password."
                };
            }

            // Login Success
            user.FailedLoginAttempts = 0;
            user.LastFailedLogin = null;
            user.LockoutEnd = null;

            _repository.User.Update(user);
            await _repository.SaveAsync();

            // Generate JWT
           string accessToken = GenerateToken(user);

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful.",
                AccessToken = accessToken
            };
        }
        private string GenerateToken(User user)
{
    var claims = new[]
    {
        new Claim("UserId", user.Id.ToString()),
        new Claim("PersonId", user.PersonId.ToString()),
        new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
    };

    string? tokenKey = _configuration["Tokens:Key"];
    string? issuer = _configuration["Tokens:Issuer"];

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(tokenKey!));

    var creds = new SigningCredentials(
        key,
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: issuer,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
    }
}