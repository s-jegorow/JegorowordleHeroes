using Microsoft.AspNetCore.Identity;

namespace JegoroWordleHeroes.Areas.Identity.Data;

public class JegorowordleHeroesUser : IdentityUser
{
    public int Score { get; set; }
    public int Avatar { get; set; }
}