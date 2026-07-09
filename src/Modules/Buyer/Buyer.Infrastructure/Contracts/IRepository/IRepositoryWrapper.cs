namespace Contracts.IRepository
{
    /// <summary>
    /// Repository Wrapper class holding every instance of repository.
    /// </summary>
    public interface IRepositoryWrapper
    {
         IBuyerBusinessProfileRepository BuyerBusinessProfile {get;} 
       

      
        bool Save();
        Task<bool> SaveAsync();
    }
}