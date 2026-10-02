using System.Globalization;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Rules shared by the weekly bucket use cases: the caller's outlet and property, the business week,
    /// the active bucket of a property, availability of a line, the read-only rule and the audit row.
    /// </summary>
    public static class WeeklyBucketRules
    {
        // The outlet the caller works with. Nothing about it is taken from the request except the outlet id,
        // and that id must be one of the caller's outlets (any outlet of the buyer when the caller has no assignment).
        public static async Task<BuyerOutlet> ResolveOutletAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid buyerId,
            Guid userId,
            Guid? outletId,
            CancellationToken cancellationToken)
        {
            List<BuyerOutlet> buyerOutlets = await repository.WeeklyBucket.ListOutletsAsync(buyerId, cancellationToken);
            List<Guid> assignedOutletIds = await repository.BuyerOutletUserMapping
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .Select(x => x.OutletId)
                .ToListAsync(cancellationToken);
            List<BuyerOutlet> callerOutlets = buyerOutlets
                .Where(x => assignedOutletIds.Contains(x.Id))
                .ToList();

            BuyerOutlet? outlet;
            if (outletId != null && outletId != Guid.Empty)
            {
                outlet = buyerOutlets.FirstOrDefault(x => x.Id == outletId);
                if (outlet == null)
                {
                    logger.LogError($"Outlet not found. OutletId: {outletId}, BuyerId: {buyerId}");
                    throw new NotFoundCustomException("Outlet not found.", "Select an outlet that belongs to this buyer organization.");
                }

                if (callerOutlets.Count > 0 && !callerOutlets.Any(x => x.Id == outlet.Id))
                {
                    logger.LogError($"Outlet is not assigned to the user. OutletId: {outlet.Id}, UserId: {userId}");
                    throw new BadRequestCustomException("Outlet is not assigned to you.", "Select one of your outlets.");
                }
            }
            else
            {
                if (callerOutlets.Count == 0)
                {
                    logger.LogError($"User has no outlet and sent none. UserId: {userId}, BuyerId: {buyerId}");
                    throw new BadRequestCustomException("Outlet is required.", "You are not assigned to an outlet. Choose an outlet.");
                }

                if (callerOutlets.Select(x => x.PropertyId).Distinct().Count() > 1)
                {
                    logger.LogError($"User works with outlets of more than one property and sent no outlet. UserId: {userId}");
                    throw new BadRequestCustomException("Outlet is required.", "Your outlets belong to more than one property. Choose an outlet.");
                }

                outlet = callerOutlets[0];
            }

            if (outlet.PropertyId == null || outlet.PropertyId == Guid.Empty)
            {
                logger.LogError($"Outlet has no property. OutletId: {outlet.Id}, BuyerId: {buyerId}");
                throw new BadRequestCustomException(
                    "This outlet is not linked to a property",
                    $"Ask your buyer administrator to link the outlet '{outlet.OutletName}' to a property.");
            }

            return outlet;
        }

        public static async Task<BuyerProperty> GetPropertyAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid propertyId,
            Guid buyerId,
            CancellationToken cancellationToken)
        {
            BuyerProperty? property = await repository.WeeklyBucket.GetPropertyAsync(propertyId, buyerId, cancellationToken);
            if (property == null)
            {
                logger.LogError($"Property not found. PropertyId: {propertyId}, BuyerId: {buyerId}");
                throw new NotFoundCustomException("Property not found.", "The property of this outlet does not exist for this buyer organization.");
            }

            return property;
        }

        // ISO week of today (UTC) moved by the configured number of weeks. The year is the year of that week.
        public static (int Year, int WeekNumber) GetCurrentWeek(IConfiguration configuration)
        {
            int offset = int.TryParse(
                configuration[Common.WEEKLY_BUCKET_WEEK_NUMBER_OFFSET],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int configuredOffset)
                ? configuredOffset
                : 0;
            DateTime day = DateTime.UtcNow.Date.AddDays(7 * offset);
            return (ISOWeek.GetYear(day), ISOWeek.GetWeekOfYear(day));
        }

        // The active bucket of a property: the bucket of the current week when it is OPEN or not created yet;
        // when that week is already frozen, the next week, and so on. Bucket is null when the week has no bucket yet.
        public static async Task<(WeeklyBucket? Bucket, int Year, int WeekNumber)> FindActiveBucketAsync(
            IRepositoryWrapper repository,
            IConfiguration configuration,
            Guid buyerId,
            Guid propertyId,
            CancellationToken cancellationToken)
        {
            (int year, int weekNumber) = GetCurrentWeek(configuration);
            List<WeeklyBucket> buckets = await repository.WeeklyBucket.ListFromWeekAsync(
                buyerId, propertyId, year, weekNumber, cancellationToken);

            while (true)
            {
                WeeklyBucket? bucket = buckets.FirstOrDefault(x => x.Year == year && x.WeekNumber == weekNumber);
                if (bucket == null || bucket.Status == Common.WEEKLY_BUCKET_OPEN)
                {
                    return (bucket, year, weekNumber);
                }

                DateTime nextWeek = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Monday).AddDays(7);
                year = ISOWeek.GetYear(nextWeek);
                weekNumber = ISOWeek.GetWeekOfYear(nextWeek);
            }
        }

        public static string BuildBucketCode(int weekNumber, string plantCode)
        {
            return $"{weekNumber}WB-{plantCode}";
        }

        public static string GetAvailability(decimal? supplierStock, decimal quantity)
        {
            if (supplierStock == null)
            {
                return Common.AVAILABILITY_UNKNOWN;
            }

            if (supplierStock <= 0)
            {
                return Common.AVAILABILITY_UNAVAILABLE;
            }

            return supplierStock >= quantity ? Common.AVAILABILITY_AVAILABLE : Common.AVAILABILITY_PARTIAL;
        }

        public static bool IsShort(string availabilityStatus)
        {
            return availabilityStatus == Common.AVAILABILITY_PARTIAL || availabilityStatus == Common.AVAILABILITY_UNAVAILABLE;
        }

        // A bucket that left the OPEN status is read-only for ever.
        public static void EnsureOpen(WeeklyBucket bucket, ILoggerManager logger)
        {
            if (bucket.Status != Common.WEEKLY_BUCKET_OPEN)
            {
                logger.LogError($"Weekly bucket is frozen. WeeklyBucketId: {bucket.Id}, Status: {bucket.Status}");
                throw new BadRequestCustomException(
                    "Weekly bucket is frozen",
                    $"The weekly bucket {bucket.BucketCode} is read-only. New requests go to the next week's bucket.");
            }
        }

        public static void AddAudit(IRepositoryWrapper repository, Guid weeklyBucketId, Guid? actorUserId, string action, string? detail)
        {
            repository.WeeklyBucketAudit.Create(new WeeklyBucketAudit
            {
                Id = Guid.NewGuid(),
                WeeklyBucketId = weeklyBucketId,
                Action = action,
                Detail = detail,
                ActorUserId = actorUserId
            });
        }
    }
}
