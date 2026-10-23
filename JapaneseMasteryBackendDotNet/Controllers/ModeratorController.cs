using JapaneseMasteryBackendDotNet.Data;
using JapaneseMasteryBackendDotNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JapaneseMasteryBackendDotNet.Controllers;

public class ApproveRequest
{
    public bool Approve { get; set; }
}

[ApiController]
[Route("api/moderator")]
public class ModeratorController : ControllerBase
{
    private readonly AppDbContext _context;

    public ModeratorController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{userId}/pending-tests")]
    public async Task<IActionResult> GetPendingTests([FromRoute] long userId)
    {
        var tests = await _context.CustomTests
            .Include(t => t.Sensei)
            .Include(t => t.Questions)
            .Where(t => !t.IsApproved)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.TestCode,
                t.MinLevel,
                t.RewardGcoin,
                t.MaxAttempts,
                t.IsApproved,
                AuthorName = t.Sensei != null ? t.Sensei.Username : "Sensei"
            })
            .ToListAsync();

        return Ok(tests);
    }

    [HttpGet("{userId}/approved-tests")]
    public async Task<IActionResult> GetApprovedTests([FromRoute] long userId)
    {
        var tests = await _context.CustomTests
            .Include(t => t.Sensei)
            .Include(t => t.Questions)
            .Where(t => t.IsApproved)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.TestCode,
                t.MinLevel,
                t.RewardGcoin,
                t.MaxAttempts,
                t.IsApproved,
                AuthorName = t.Sensei != null ? t.Sensei.Username : "Sensei"
            })
            .ToListAsync();

        return Ok(tests);
    }

    [HttpGet("{userId}/pending-items")]
    public async Task<IActionResult> GetPendingItems([FromRoute] long userId)
    {
        var items = await _context.ShopItems
            .Include(i => i.Designer)
            .Where(i => !i.IsApproved)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPut("{userId}/tests/{testId}/approve")]
    public async Task<IActionResult> ProcessTest([FromRoute] long userId, [FromRoute] long testId, [FromBody] ApproveRequest req)
    {
        var test = await _context.CustomTests.FindAsync(testId);
        if (test == null) return NotFound("Test not found");

        if (req.Approve)
        {
            test.IsApproved = true;
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.CustomTestQuestions.RemoveRange(_context.CustomTestQuestions.Where(q => q.CustomTestId == testId));
            _context.CustomTests.Remove(test);
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpPut("{userId}/items/{itemId}/approve")]
    public async Task<IActionResult> ProcessItem([FromRoute] long userId, [FromRoute] long itemId, [FromBody] ApproveRequest req)
    {
        var item = await _context.ShopItems.FindAsync(itemId);
        if (item == null) return NotFound("Item not found");

        if (req.Approve)
        {
            item.IsApproved = true;
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.ShopItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        return Ok();
    }
}

