using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodeGraph.Api.Data;
using NodeGraph.Api.Data.Entities;
using NodeGraph.Api.DTOs;

namespace NodeGraph.Api.Controllers;

/// <summary>
/// グラフデータのCRUD操作を提供するAPIコントローラ
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GraphsController : ControllerBase
{
    private readonly GraphDbContext _context;

    public GraphsController(GraphDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// グラフ一覧を取得
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GraphMetadataDto>>> GetGraphs()
    {
        var graphs = await _context.Graphs
            .OrderByDescending(g => g.ModifiedAt)
            .Select(g => new GraphMetadataDto(g.Id, g.Name, g.CreatedAt, g.ModifiedAt))
            .ToListAsync();

        return Ok(graphs);
    }

    /// <summary>
    /// 指定IDのグラフを取得
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GraphDataDto>> GetGraph(string id)
    {
        var graph = await _context.Graphs.FindAsync(id);

        if (graph == null)
        {
            return NotFound();
        }

        return Ok(new GraphDataDto(
            graph.Id,
            graph.Name,
            graph.GraphYaml,
            graph.LayoutYaml,
            graph.CreatedAt,
            graph.ModifiedAt
        ));
    }

    /// <summary>
    /// 新規グラフを作成
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SaveGraphResponse>> CreateGraph(SaveGraphRequest request)
    {
        var now = DateTime.UtcNow;
        var entity = new GraphEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name,
            GraphYaml = request.GraphYaml,
            LayoutYaml = request.LayoutYaml,
            CreatedAt = now,
            ModifiedAt = now
        };

        _context.Graphs.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetGraph),
            new { id = entity.Id },
            new SaveGraphResponse(entity.Id, entity.CreatedAt, entity.ModifiedAt)
        );
    }

    /// <summary>
    /// 既存グラフを更新
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SaveGraphResponse>> UpdateGraph(string id, SaveGraphRequest request)
    {
        var entity = await _context.Graphs.FindAsync(id);

        if (entity == null)
        {
            return NotFound();
        }

        entity.Name = request.Name;
        entity.GraphYaml = request.GraphYaml;
        entity.LayoutYaml = request.LayoutYaml;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new SaveGraphResponse(entity.Id, entity.CreatedAt, entity.ModifiedAt));
    }

    /// <summary>
    /// グラフを削除
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGraph(string id)
    {
        var entity = await _context.Graphs.FindAsync(id);

        if (entity == null)
        {
            return NotFound();
        }

        _context.Graphs.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// グラフ名を変更
    /// </summary>
    [HttpPatch("{id}/name")]
    public async Task<IActionResult> RenameGraph(string id, RenameGraphRequest request)
    {
        var entity = await _context.Graphs.FindAsync(id);

        if (entity == null)
        {
            return NotFound();
        }

        entity.Name = request.NewName;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
