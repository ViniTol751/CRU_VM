namespace TesteAPI.Models;

public interface ILocalSyncEntity
{
    int Id { get; set; }
    DateTime UpdatedAt { get; set; }
    bool IsDeleted { get; set; }
}