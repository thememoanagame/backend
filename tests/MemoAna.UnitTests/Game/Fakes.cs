using System.Linq.Expressions;
using FluentValidation;
using MemoAna.Application.Common.Abstractions;
using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Game.Dtos;
using MemoAna.Domain.Common;
using MemoAna.Domain.Game;

namespace MemoAna.UnitTests.Game;

internal sealed class FakeGameThemeService : IThemeService
{
    public GameThemeDto? ReturnData { get; set; } = new("themeId", "themeName", "thumbId", ["image01Id", "image02Id"]);
    public Task<GameThemeDto> AddThemeAsync(string name, Stream logoStream, string logoFilename, IEnumerable<(string Filename, Stream Stream)> cardStreams, CancellationToken cancellationToken = default) =>
        Task.FromResult(ReturnData!);

    public Task<bool> DeleteThemeAsync(string themeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<IEnumerable<GameThemeDto>> FindThemesAsync(Expression<Func<Theme, bool>> predicate, CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<GameThemeDto>>([ReturnData!]);
        
    public Task<GameThemeDto> UpdateThemeAsync(string id, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(ReturnData!);
}

internal sealed class FakeRelationalRepository<TEntity>
    : IRepository<TEntity>
    where TEntity : EntityBase
{
    public Dictionary<string, TEntity> Items { get; } = [];

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? predicate = null, bool tracking = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Values.FirstOrDefault(predicate?.Compile() ?? (entity => true)));

    public Task<TEntity?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.GetValueOrDefault(id));

    public Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null, 
        bool track = false,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<TEntity> items = Items.Values;
        if (predicate is not null)
        {
            items = items.Where(predicate.Compile());
        }

        return Task.FromResult<IReadOnlyList<TEntity>>([.. items]);
    }

    public Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        Items[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        bool exists = Items.ContainsKey(entity.Id);
        if (exists)
        {
            Items[entity.Id] = entity;
        }

        return Task.FromResult(exists);
    }

    public Task<bool> RemoveAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Remove(id));
}

