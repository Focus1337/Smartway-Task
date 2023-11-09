using FileHub.Core.Models;

namespace FileHub.Core.Interfaces;

/// <summary>
/// <para>
/// <c>IFileGroupRepository</c> для <see cref="FileGroup"/>. Предоставляет функционал для работы с <see cref="FileMeta"/> в БД.
/// </para>
/// </summary>
public interface IFileGroupRepository
{
    /// <summary>
    /// Получить группу файлов пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <returns>Группа файлов</returns>
    Task<FileGroup?> GetFileGroupAsync(Guid userId, Guid groupId);

    /// <summary>
    /// Получить список групп файлов пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Список групп файлов </returns>
    Task<List<FileGroup>> GetListOfGroupsAsync(Guid userId);

    /// <summary>
    /// Создать <see cref="FileGroup"/>.
    /// </summary>
    /// <param name="fileGroup">Группа файлов</param>
    Task CreateFileGroupAsync(FileGroup fileGroup);
}