using FileHub.Core.Models;

namespace FileHub.Presentation.Models;

public class FileGroupDto
{
    public Guid Id { get; set; }
    public List<FileMeta> FileMetas { get; set; }

    public FileGroupDto(Guid id, List<FileMeta> fileMetas)
    {
        Id = id;
        FileMetas = fileMetas;
    }

    public static FileGroupDto EntityToDto(FileGroup model) =>
        new(model.Id, model.FileMetas);
}