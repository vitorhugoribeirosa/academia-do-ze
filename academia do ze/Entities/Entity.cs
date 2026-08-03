// VITOR HUGO RIBEIRO SA
namespace AcademiaDoZe.Domain.Entities;

public abstract class Entity
{
    public int Id { get; protected set; }

    protected Entity(int id = 0)
    {
        Id = id;
    }
}
