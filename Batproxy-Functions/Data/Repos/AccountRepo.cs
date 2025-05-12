using Batproxy_API.Repository.Entities;
using Batproxy_Functions.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Batproxy_API.Repository.Repos
{
    public class AccountRepo
    {
        private readonly SQLContext _context;
        public AccountRepo(SQLContext sqlContext)
        {
            _context = sqlContext;
        }

        #region Login & Register
        public async Task Register(string email, string hashedPassword, string registeredIP, string tier, Guid verificationToken, DateTimeOffset verificationExpiry)
        {
            var account = new AccountEntity
            {
                Email = email,
                Password = hashedPassword,
                RegisteredIP = registeredIP,
                Tier = tier,
                CreatedDate = DateTime.UtcNow,
                VerificationToken = verificationToken,
                VerificationExpiry = verificationExpiry,
                IsVerified = false
            };

            _context.Accounts.Add(account);

            await _context.SaveChangesAsync();
        }

        public async Task<AccountEntity?> Login(string email)
        {
            return await _context.Accounts.SingleOrDefaultAsync(a => a.Email == email);
        }
        #endregion


        #region Existence Checks
        public async Task<bool> EmailExists(string email)
        {
            return await _context.Accounts.AnyAsync(a => a.Email == email);
        }

        public async Task<bool> IPTrialUsed(string ip)
        {
            return await _context.Accounts.AnyAsync(a => a.RegisteredIP == ip && a.Tier == "freetrial");
        }
        #endregion

        public async Task<AccountEntity?> Verify(Guid token)
        {
            var now = DateTimeOffset.UtcNow;

            var acct = await _context.Accounts
                .Where(a => a.VerificationToken == token
                         && !a.IsVerified
                         && a.VerificationExpiry > now)
                .SingleOrDefaultAsync();

            if (acct == null)
                return null;

            acct.IsVerified = true;
            await _context.SaveChangesAsync();

            return await _context.Accounts.Where(a => a.VerificationToken == token).SingleOrDefaultAsync();
        }

        public async Task ClearTable()
        {
            // TRUNCATE isn't directly exposed by EF, so issue raw SQL:
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Accounts;");
        }
    }
}
