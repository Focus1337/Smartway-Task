using FileHub.Core.Models;

namespace FileHub.Core.Interfaces;

/// <summary>
/// <para>
/// <c>IFileMetaRepository</c> для <see cref="FileMeta"/>. Предоставляет функционал для работы с <see cref="FileMeta"/> в БД.
/// </para>
/// </summary>
public interface IFileMetaRepository
{
    /// <summary>
    /// Получить список метаданных файлов пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Список метаданных файлов</returns>
    Task<List<FileMeta>> GetListOfFilesAsync(Guid userId);

    /// <summary>
    /// Получить метаданные файла пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <param name="fileId">ID файла</param>
    /// <returns>Метаданные файла</returns>
    Task<FileMeta?> GetFileMetaAsync(Guid userId, Guid groupId, Guid fileId);
}