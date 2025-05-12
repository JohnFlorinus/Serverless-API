using System.ComponentModel.DataAnnotations;

namespace Batproxy_API.Repository.Entities
    {
        public class AccountEntity
        {
            [Key]
            public int AccountID { get; set; }

            [EmailAddress]
            [StringLength(100)]
            public required string Email { get; set; }

            [StringLength(60)] // BCrypt hash = 60 chars
            public required string Password { get; set; }

            [StringLength(50)]
            public required string Tier { get; set; }

            [StringLength(50)]
            public required string RegisteredIP { get; set; }

            public DateTime CreatedDate { get; set; }
            public bool TrialExpired { get; set; }
            public Guid VerificationToken { get; set; }
            public DateTimeOffset VerificationExpiry { get; set; }
            public bool IsVerified { get; set; }
        }
}
