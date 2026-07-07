using Contracts.IRepository;
using Identity.Infrastructure.DbContext;
using Microsoft.Extensions.Configuration;
using Contracts;
using SharedKernel.LoggerServices;
using Identity.Infrastructure.Contracts.IServices;
using Identity.Domain.Entities;


namespace Repository
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private readonly RepositoryContext _context;
        private readonly IUserIdentityService _userIdentityService;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;
        private readonly string _dbConnectionString;
        private IEmailVerificationRepository _emailVerificationRepository;
        private IOrganizationRepository _organizationRepository;
        private IUserRepository _userRepository;
        private IPersonRepository _personRepository;
        private IUserRoleMappingRepository _userRoleMappingRepository;
        private IRoleRepository _roleRepository;
   
        private IRefreshTokenRepository _refreshTokenRepository;
        private ILoginRecordRepository _loginRecordRepository;
        private IRoleFeatureMappingRepository _roleFeatureMapping;
        private IFeatureRepository _featureRepository;

        public RepositoryWrapper(RepositoryContext repositoryContext, IUserIdentityService userIdentityService, IConfiguration configuration, ILoggerManager logger)
        {
            _context = repositoryContext;
            _userIdentityService = userIdentityService;
            _logger = logger;
            _configuration = configuration;
            _dbConnectionString = configuration.GetConnectionString("DefaultConnection")!;
        }
          public IEmailVerificationRepository EmailVerification
        {
            get
            {
                if (_emailVerificationRepository == null)
                {
                    _emailVerificationRepository = new EmailVerificationRepository(_context);
                }
                return _emailVerificationRepository;
            }
        }
        public IOrganizationRepository Organization
        {
            get
            {
                if (_organizationRepository == null)
                {
                    _organizationRepository = new OrganizationRepository(_context);
                }
                return _organizationRepository;
            }
        }
        public IUserRepository User
        {
            get
            {
                if (_userRepository == null)
                {
                    _userRepository = new UserRepository(_context);
                }
                return _userRepository;
            }
        }
        public IPersonRepository Person
        {
            get
            {
                if (_personRepository == null)
                {
                    _personRepository = new PersonRepository(_context);
                }
                return _personRepository;
            }
        }
        public IUserRoleMappingRepository UserRoleMapping
        {
            get
            {
                if (_userRoleMappingRepository == null)
                {
                    _userRoleMappingRepository = new UserRoleMappingRepository(_context);
                }
                return _userRoleMappingRepository;
            }
        }
        public IRoleRepository Role
        {
            get
            {
                if (_roleRepository == null)
                {
                    _roleRepository = new RoleRepository(_context);
                }
                return _roleRepository;
            }
        }
      
        public IRefreshTokenRepository RefreshToken
        {
            get
            {
                if (_refreshTokenRepository == null)
                {
                    _refreshTokenRepository = new RefreshTokenRepository(_context);
                }
                return _refreshTokenRepository;
            }
        }
        public ILoginRecordRepository LoginRecord
        {
            get
            {
                if (_loginRecordRepository == null)
                {
                    _loginRecordRepository = new LoginRecordRepository(_context);
                }
                return _loginRecordRepository;
            }
        }
        public IRoleFeatureMappingRepository RoleFeatureMapping
        {
            get
            {
                if(_roleFeatureMapping ==null)
                {
                    _roleFeatureMapping =new RoleFeatureMappingRepository(_context);
                }
                return _roleFeatureMapping;
            }
        }
        public IFeatureRepository Feature
        {
            get
            {
                if(_featureRepository ==null)
                {
                    _featureRepository =new FeatureRepository(_context);
                }
                return _featureRepository;
            }
        }

        public bool Save()
        {
            _context.OnBeforeSaving(_userIdentityService.GetCurrentUser());
            _context.SaveChanges();
            return true;
        }

        public async Task<bool> SaveAsync()
        {
            _context.OnBeforeSaving(_userIdentityService.GetCurrentUser());
            await _context.SaveChangesAsync();
            return true;
        }
    }
}