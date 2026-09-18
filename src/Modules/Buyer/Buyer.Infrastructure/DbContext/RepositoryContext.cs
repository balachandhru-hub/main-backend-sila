using Microsoft.EntityFrameworkCore;
using Buyer.Domain.Entities;
using SharedKernel.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using SharedKernel.Util;
using Buyer.Domain.Common;

namespace Buyer.Infrastructure.DbContext
{
    public class RepositoryContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly IConfiguration _configuration;

        public RepositoryContext(DbContextOptions<RepositoryContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }
        public DbSet<BuyerCategory> BuyerCategory { get; set; }
        public DbSet<BuyerDeliveryLocation> BuyerDeliveryLocation { get; set; }
        public DbSet<BuyerBankAccount> BuyerBankAccount { get; set; }
        public DbSet<BuyerBusinessProfile> BuyerBusinessProfile { get; set; }
        public DbSet<BuyerRegistration> BuyerRegistration { get; set; }
        public DbSet<Asset> Asset { get; set; }
        public DbSet<RFQ> RFQ { get; set; }
        public DbSet<RFQItem> RFQItem { get; set; }
        public DbSet<RFQAttachmentMapping> RFQAttachmentMapping { get; set; }
        public DbSet<RFQItemAttachmentMapping> RFQItemAttachmentMapping { get; set; }
        public DbSet<ItemBuyerMaster> ItemBuyerMaster { get; set; }
        public DbSet<RFQQuestionAnswer> RFQQuestionAnswer { get; set; }
        public DbSet<RFQQuestion> RFQQuestion { get; set; }
        public DbSet<RFQQuestionOption> RFQQuestionOption { get; set; }
        public DbSet<SupplierVerificationRequest> SupplierVerificationRequest { get; set; }
        public DbSet<VerificationAnswer> VerificationAnswer { get; set; }
        public DbSet<VerificationAnswerOption> VerificationAnswerOption { get; set; }
        public DbSet<VerificationTemplate> VerificationTemplate { get; set; }
        public DbSet<VerificationTemplateQuestion> VerificationTemplateQuestion { get; set; }
        public DbSet<VerificationTemplateQuestionOption> VerificationTemplateQuestionOption { get; set; }
        public DbSet<RFQAnswerOption> RFQAnswerOption { get; set; }
        public DbSet<BuyerCostCenter> BuyerCostCenter {get;set;}
        public DbSet<BuyerDepartment> BuyerDepartment {get;set;}
        public DbSet<BuyerSupplierMapping> BuyerSupplierMapping {get;set;}
        public DbSet<RFQSupplierMapping> RFQSupplierMapping {get;set;}
        public DbSet<DefaultVerificationTemplate> DefaultVerificationTemplate {get;set;}
        public DbSet<DefaultVerificationTemplateQuestion> DefaultVerificationTemplateQuestion {get;set;}
        public DbSet<RFQQuestionAttachmentMapping> RFQQuestionAttachmentMapping {get;set;}
        public DbSet<RFQBlockchainRecord> RFQBlockchainRecord {get;set;}
        public DbSet<RFQOrganizationUserMapping> RFQOrganizationUserMapping {get;set;}
        public DbSet<ExternalSupplier> ExternalSupplier {get;set;}
        public DbSet<RFQExternalSupplier> RFQExternalSupplier {get;set;}
        public DbSet<MessageThread> MessageThread {get;set;}
        public DbSet<Message> Message {get;set;}
        public DbSet<MessageAttachment> MessageAttachment {get;set;}
        public DbSet<PredefinedMaterial> PredefinedMaterial {get;set;}
        public DbSet<MasterApprovalFlow> MasterApprovalFlow {get;set;}
        public DbSet<ApprovalFlowUserMapping> ApprovalFlowUserMapping {get;set;}
        public DbSet<PredefinedMaterialApprovalFlowUserMapping> PredefinedMaterialApprovalFlowUserMapping {get;set;}
        public DbSet<ApprovalFlowPredefinedMaterialMapping> ApprovalFlowPredefinedMaterialMapping {get;set;}
        public DbSet<RFQAward> RFQAward {get;set;}
        public DbSet<RFQAwardItem> RFQAwardItem {get;set;}
        public DbSet<Contract> Contract {get;set;}
        public DbSet<ContractAttachment> ContractAttachment {get;set;}
        public DbSet<ContractApprovalFlow> ContractApprovalFlow {get;set;}
        public DbSet<ContractApprovalUserMapping> ContractApprovalUserMapping {get;set;}




        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            _ = modelBuilder.HasDefaultSchema(_configuration[Common.APPLICATION_SCHEMA]);
            _ = modelBuilder.Entity<BuyerCategory>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerBankAccount>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerBusinessProfile>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerDeliveryLocation>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerRegistration>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Asset>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQ>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQAttachmentMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQItemAttachmentMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQQuestion>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQQuestionAnswer>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQAnswerOption>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQQuestionOption>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ItemBuyerMaster>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SupplierVerificationRequest>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<VerificationAnswer>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<VerificationAnswerOption>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<VerificationTemplate>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<VerificationTemplateQuestion>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<VerificationTemplateQuestionOption>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerDepartment>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<BuyerCostCenter>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<BuyerSupplierMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQSupplierMapping>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<DefaultVerificationTemplateQuestion>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<DefaultVerificationTemplate>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQQuestionAttachmentMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQBlockchainRecord>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQOrganizationUserMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<ExternalSupplier>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQExternalSupplier>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<MessageThread>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<Message>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<MessageAttachment>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<PredefinedMaterial>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<MasterApprovalFlow>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<ApprovalFlowUserMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<PredefinedMaterialApprovalFlowUserMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<ApprovalFlowPredefinedMaterialMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQAward>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQAwardItem>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<Contract>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<ContractAttachment>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<ContractApprovalFlow>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<ContractApprovalUserMapping>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.HasSequence<long>(
                Common.CONTRACT_NUMBER_SEQUENCE,
                _configuration[Common.APPLICATION_SCHEMA]!);







            base.OnModelCreating(modelBuilder);

            foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
            {

                entity.SetTableName(entity.GetTableName()!.ConvertToSnakeCase());
                var storeObjectIdentifier = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableProperty property in entity.GetProperties())
                {

                    property.SetColumnName(property.GetColumnName(storeObjectIdentifier)!.ConvertToSnakeCase());
                }

                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableKey key in entity.GetKeys())
                {
                    key.SetName(key.GetName()!.ConvertToSnakeCase());
                }

                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey key in entity.GetForeignKeys())
                {
                    key.SetConstraintName(key.GetConstraintName()!.ConvertToSnakeCase());
                }

                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableIndex index in entity.GetIndexes())
                {
                    index.SetDatabaseName(index.GetDatabaseName()!.ConvertToSnakeCase());
                }
            }
        }
        public void OnBeforeSaving(Guid UserId)
        {
            System.Collections.Generic.IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> entries = ChangeTracker.Entries();
            foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry in entries)
            {
                if (entry.Entity is BaseModel trackable)
                {
                    DateTime now = DateTime.UtcNow;
                    Guid user = UserId;
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            trackable.DateUpdated = now;
                            trackable.UpdatedBy = user;
                            break;
                        case EntityState.Added:
                            trackable.DateCreated = now;
                            trackable.CreatedBy = user;
                            trackable.DateUpdated = now;
                            trackable.UpdatedBy = user;
                            trackable.IsActive = true;
                            break;
                        case EntityState.Detached:
                            break;
                        case EntityState.Unchanged:
                            break;
                        case EntityState.Deleted:
                            break;
                        default:
                            break;
                    }
                }
            }
        }




    }
}