using JegoroWordleHeroes.Areas.Identity.Data;
using JegoroWordleHeroes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JegoroWordleHeroes.Controllers
{
    public class RankingController : Controller
    {
        private const int TopCount = 10;
        private readonly JegorowordleHeroesContext _context;

        public RankingController(JegorowordleHeroesContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var myName = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;

            var topUsers = await _context.Users
                .Where(u => u.Score > 0)
                .OrderByDescending(u => u.Score)
                .ThenBy(u => u.UserName)
                .Take(TopCount)
                .Select(u => new { u.UserName, u.Score, u.Avatar })
                .ToListAsync();

            var model = new RankingViewModel();
            var rank = 0;
            int? lastScore = null;

            for (int i = 0; i < topUsers.Count; i++)
            {
                if (topUsers[i].Score != lastScore)
                {
                    rank = i + 1;
                    lastScore = topUsers[i].Score;
                }

                model.Top.Add(new RankingEntry
                {
                    Rank = rank,
                    UserName = topUsers[i].UserName ?? "",
                    Score = topUsers[i].Score,
                    Avatar = topUsers[i].Avatar,
                    IsMe = topUsers[i].UserName == myName
                });
            }

            if (myName != null && !model.Top.Any(e => e.IsMe))
            {
                var me = await _context.Users
                    .Where(u => u.UserName == myName)
                    .Select(u => new { u.Score, u.Avatar })
                    .FirstOrDefaultAsync();

                if (me != null && me.Score > 0)
                {
                    var betterPlayers = await _context.Users.CountAsync(u => u.Score > me.Score);
                    model.Me = new RankingEntry
                    {
                        Rank = betterPlayers + 1,
                        UserName = myName,
                        Score = me.Score,
                        Avatar = me.Avatar,
                        IsMe = true
                    };
                }
            }

            return PartialView("_Ranking", model);
        }
    }
}
