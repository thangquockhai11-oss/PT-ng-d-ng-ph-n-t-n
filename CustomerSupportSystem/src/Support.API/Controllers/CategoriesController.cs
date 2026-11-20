using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Support.API.Data;
using Support.API.DTOs.Category;
using Support.Shared.Models;

namespace Support.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> GetAll() => Ok(await context.Categories.OrderBy(x => x.Name)
        .Select(x => new { x.Id, x.Name, x.Description }).ToListAsync());

    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await context.Categories.Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Name, x.Description }).FirstOrDefaultAsync();
        return item is null ? NotFound(new { message = "Không tìm thấy Category." }) : Ok(item);
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateCategoryRequest request)
    {
        var name = request.Name.Trim();
        if (await context.Categories.AnyAsync(x => x.Name.ToLower() == name.ToLower()))
            return Conflict(new { message = "Category đã tồn tại." });
        var category = new Category { Name = name, Description = request.Description?.Trim() };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, UpdateCategoryRequest request)
    {
        var category = await context.Categories.FindAsync(id);
        if (category is null) return NotFound(new { message = "Không tìm thấy Category." });
        var name = request.Name.Trim();
        if (await context.Categories.AnyAsync(x => x.Id != id && x.Name.ToLower() == name.ToLower()))
            return Conflict(new { message = "Tên Category đã tồn tại." });
        category.Name = name;
        category.Description = request.Description?.Trim();
        await context.SaveChangesAsync();
        return Ok(category);
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await context.Categories.FindAsync(id);
        if (category is null) return NotFound(new { message = "Không tìm thấy Category." });
        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        return Ok(new { message = "Xóa Category thành công." });
    }
}
