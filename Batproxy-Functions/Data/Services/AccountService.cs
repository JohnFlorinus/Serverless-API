using Batproxy_API.Repository.Entities;
using Batproxy_API.Repository.Helpers;
using Batproxy_API.Repository.Repos;
using Batproxy_Functions.Middleware;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;

namespace Batproxy_API.Repository.Services
{
    public class AccountService
    {
        private readonly AccountRepo _accountRepo;
        private readonly EncryptionHelper _encryptionHelper;
        private readonly MailHelper _mailHelper;
        private readonly JwtMiddleware _jwtMiddleware;
        public AccountService(AccountRepo accountRepo, EncryptionHelper encryptionHelper, MailHelper mailHelper, JwtMiddleware jwtMiddleware)
        {
            _accountRepo = accountRepo;
            _encryptionHelper = encryptionHelper;
            _mailHelper = mailHelper;
            _jwtMiddleware = jwtMiddleware;
        }

        public async Task<string> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new ValidationException("Email and password is required.");

            AccountEntity? account = await _accountRepo.Login(email);
            if (account == null)
                throw new InvalidDataException("There is no account with such an email.");

            bool validPass = _encryptionHelper.VerifyPassword(password, account.Password);
            if (!validPass)
                throw new ValidationException("Invalid password.");

            // kolla om kontot är verifierat
            if (!account.IsVerified)
                throw new UnauthorizedAccessException("Your account is not yet verified. Please check your email.");

            // ge ut JWT
            return await _jwtMiddleware.TokenCreate(account.AccountID,account.Email, account.Tier, account.TrialExpired, account.CreatedDate);
        }

        public async Task<string> Register(string email, string password, string tier, string ip)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new ValidationException("Email and password is required.");

            if (tier!="freetrial" && tier!="standard" && tier!="vip")
                throw new ValidationException("Invalid tier.");

            if (tier!="freetrial")
                throw new ValidationException("We have not yet implemented paid subscriptions.");

            // Dubbelkollar formattet, disposable blocklist och inte redan registrerat
            await _mailHelper.CheckEmailValid(email);

            if (tier=="freetrial")
            {
                bool ipExists = await _accountRepo.IPTrialUsed(ip);
                if (ipExists)
                    throw new ValidationException("Your IP Address has already been used for a free trial.");
            }

            string hashedPassword = _encryptionHelper.CreatePassword(password);

            // för email verifiering
            var token = Guid.NewGuid();
            var expiry = DateTimeOffset.UtcNow.AddHours(2);
            await _mailHelper.SendVerificationEmail(email, token.ToString());

            // skapa SQL-insert EFTER mail har skickats
            await _accountRepo.Register(email, hashedPassword, ip, tier, token, expiry);

            return "Your account has been registered";
        }


        public async Task<string> Verify(string token)
        {
            if (string.IsNullOrWhiteSpace(token)
                || !Guid.TryParse(token, out var guid))
                    throw new ValidationException("Invalid verification token.");

            AccountEntity account = await _accountRepo.Verify(Guid.Parse(token));
            if (account==null)
                throw new ValidationException("Verification link is invalid or has expired.");

            return await _jwtMiddleware.TokenCreate(account.AccountID, account.Email, account.Tier, account.TrialExpired, account.CreatedDate);
        }


        public async Task ClearTable()
        {
            _accountRepo.ClearTable();
        }
    }
}
