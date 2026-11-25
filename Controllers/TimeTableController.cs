using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuloPortal.Data;
using TanuloPortal.Models;

namespace TanuloPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeTableController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TimeTableController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeTableItem>>> GetTimeTable()
        {
            return await _context.TimeTableItems
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.StartTime)
                .ToListAsync();
        }

        [HttpGet("day/{dayOfWeek}")]
        public async Task<ActionResult<IEnumerable<TimeTableItem>>> GetTimeTableByDay(int dayOfWeek)
        {
            if (dayOfWeek < 1 || dayOfWeek > 7)
                return BadRequest(new { message = "A nap értéke 1 (Hétfő) és 7 (Vasárnap) között lehet." });

            return await _context.TimeTableItems
                .Where(t => t.DayOfWeek == dayOfWeek)
                .OrderBy(t => t.StartTime)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<TimeTableItem>> PostTimeTableItem(TimeTableItem item)
        {
            _context.TimeTableItems.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTimeTable), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutTimeTableItem(int id, TimeTableItem item)
        {
            if (id != item.Id)
            {
                return BadRequest();
            }

            _context.Entry(item).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TimeTableItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTimeTableItem(int id)
        {
            var item = await _context.TimeTableItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _context.TimeTableItems.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TimeTableItemExists(int id)
        {
            return _context.TimeTableItems.Any(e => e.Id == id);
        }
    }
}