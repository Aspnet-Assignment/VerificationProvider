using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VerificationProvider.Data.Contexts;

namespace VerificationProvider.Functions
{
    public class CleanTimer(ILoggerFactory loggerFactory, DataContext context)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<CleanTimer>();
        private readonly DataContext _context = context;

        [Function("CleanTimer")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            try
            {
                var expired = await _context.VerificationRequests.Where(x => x.ExpiryDate <= DateTime.Now).ToListAsync();
                _context.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : CleanTimer.Run :: {ex.Message}");
            }

        }
    }
}
