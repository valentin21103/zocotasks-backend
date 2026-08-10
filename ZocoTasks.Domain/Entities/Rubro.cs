namespace ZocoTasks.Domain.Entities;

public class Rubro
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public bool Activo { get; set; } = true;

    public ICollection<Comercio> Comercios { get; set; } = new List<Comercio>();
}
