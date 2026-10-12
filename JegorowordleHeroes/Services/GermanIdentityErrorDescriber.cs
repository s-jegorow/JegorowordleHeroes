using Microsoft.AspNetCore.Identity;

namespace JegoroWordleHeroes.Services
{
    public class GermanIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DuplicateUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = $"Der Benutzername '{userName}' ist schon vergeben."
            };
        }

        public override IdentityError InvalidUserName(string? userName)
        {
            return new IdentityError
            {
                Code = nameof(InvalidUserName),
                Description = "Der Benutzername darf nur Buchstaben, Ziffern und - . _ enthalten."
            };
        }
    }
}
