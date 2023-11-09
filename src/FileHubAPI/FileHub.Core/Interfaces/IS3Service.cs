using Amazon.S3.Model;
using FileHub.Core.Models;
using FluentResults;
using Microsoft.AspNetCore.Http;

namespace FileHub.Core.Interfaces;

/// <summary>
/// <para>
/// Сервис <c>IS3Service</c> для работы с S3 хранилищем.
/// </para>
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Получить объект из S3 хранилища.
    /// </summary>
    /// <param name="ownerId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <param name="fileId">ID файла</param>
    /// <returns>S3 Объект</returns>
    Task<Result<GetObjectResponse>> GetFileByIdAsync(Guid ownerId, Guid groupId, Guid fileId);

    /// <summary>
    /// Получить список объектов, входящие в одну группу, из S3 хранилища.
    /// </summary>
    /// <param name="ownerId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <returns>Список S3 объектов</returns>
    Task<Result<List<GetObjectResponse>>> GetGroupByIdAsync(Guid ownerId, Guid groupId);

    /// <summary>
    /// Получить прогресс загрузки определенного файла.
    /// </summary>
    /// <param name="ownerId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <param name="fileId">ID файла</param>
    /// <returns>Прогресс загрузки файла в S3</returns>
    Task<Result<int>> GetFileUploadProgress(Guid ownerId, Guid groupId, Guid fileId);

    /// <summary>
    /// Получить прогресс загрузки группы файлов.
    /// </summary>
    /// <param name="ownerId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <returns>Прогресс загрузки группы файлов в S3</returns>
    Task<Result<int>> GetGroupUploadProgress(Guid ownerId, Guid groupId);

    /// <summary>
    /// Загрузить группу файлов в S3 хранилище.
    /// </summary>
    /// <param name="ownerId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <param name="files">Список файлов, которые нужно загрузить</param>
    /// <param name="fileGuids">Заранее сгенерированный список ID файлов</param>
    /// <returns>ID группы и список ID файлов (S3 объектов)</returns>
    Task<Result<UploadedFileGroup>> UploadGroupAsync(Guid ownerId, Guid groupId, List<IFormFile> files,
        List<Guid> fileGuids);

    /// <summary>
    /// Открыть доступ к группе S3 объектов. Создается <c>Zip</c> архив из группы S3 объектов и предоставляется ссылка на этот архив.
    /// </summary>
    /// <param name="ownerId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <returns>Временная ссылка на архив из группы S3 объектов</returns>
    Task<Result<string>> ShareGroupAsync(Guid ownerId, Guid groupId);

    /// <summary>
    /// Открыть доступ к определенному S3 объекту. Предоставляется ссылка на этот объект.
    /// </summary>
    /// <param name="ownerId">ID пользователя</param>
    /// <param name="groupId">ID группы</param>
    /// <param name="fileId">ID файла</param>
    /// <returns>Временная ссылка на S3 объект</returns>
    Task<Result<string>> ShareFileAsync(Guid ownerId, Guid groupId, Guid fileId);

    /// <summary>
    /// Архивировать S3 объекты в <c>Zip</c> файл.
    /// </summary>
    /// <param name="stream">Выходной поток</param>
    /// <param name="files">Список S3 объектов</param>
    /// <param name="leaveOpen">Оставить поток открытым. Default: false</param>
    Task ZipFiles(Stream stream, List<GetObjectResponse> files, bool leaveOpen = false);
}