using Batproxy_API.Repository.Entities;
using Batproxy_API.Repository.Repos;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Batproxy_API.Repository.Helpers
{
    public class EncryptionHelper
    {
        #region Hashing + Salting
        public string CreatePassword(string password)
        {
            string salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);
            return hashedPassword;
        }

        public bool VerifyPassword(string input, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(input, hash);
        }
        #endregion
    }
}
