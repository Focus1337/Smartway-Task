using FileHub.Core.Models;
using FluentResults;

namespace FileHub.Core.Interfaces;

/// <summary>
/// <para>
/// Сервис <c>IFileService</c> используется для работы с группами метаданных файлов <see cref="FileGroup"/> или отдельными
/// метаданными файлов <see cref="FileMeta"/>.
/// </para>
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Получить метаданные файла <see cref="FileMeta"/> пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <param name="fileId">ID файла</param>
    /// <returns>Метаданные файла</returns>
    Task<Result<FileMeta>> GetFileMetaAsync(Guid userId, Guid groupId, Guid fileId);

    /// <summary>
    /// Получить группу файлов <see cref="FileGroup"/> пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <returns>Группа файлов</returns>
    Task<Result<List<FileMeta>>> GetFileGroupAsync(Guid userId, Guid groupId);

    /// <summary>
    /// Получить список всех метаданных файлов <see cref="FileMeta"/> пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Список метаданных файлов</returns>
    Task<Result<List<FileMeta>>> GetListOfFiles(Guid userId);

    /// <summary>
    /// Получить список всех групп файлов <see cref="FileGroup"/> паользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Список групп файлов</returns>
    Task<Result<List<FileGroup>>> GetListOfGroups(Guid userId);

    /// <summary>
    /// Создать <see cref="FileGroup"/>.
    /// </summary>
    /// <param name="fileGroup">Группа файлов</param>
    Task CreateFileGroupAsync(FileGroup fileGroup);
}