using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JegoroWordleHeroes.Areas.Identity.Data;

public class JegorowordleHeroesContext : IdentityDbContext<JegorowordleHeroesUser>
{
    public JegorowordleHeroesContext(DbContextOptions<JegorowordleHeroesContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}