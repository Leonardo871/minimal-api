using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MinimalAPI.Dominio.Entidades;

namespace MinimalAPI.Infraestrutura.Db;

public class DbContexto : DbContext
{
    private readonly IConfiguration _configuracaoAppSettings;
    public DbContexto(IConfiguration configuracaoAppSettings )
    {
        _configuracaoAppSettings = configuracaoAppSettings;
    }
    public DbSet<Administrador> Administradores {get; set; } = default!;
    public DbSet<Veiculo>Veiculos {get; set; } = default!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administrador>().HasData(
                new Administrador{
                    Id = 1 ,
                    Email = "Administrador@teste.com",
                    Senha = "123456",
                    Perfil = "Adm"
                }
        );
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _configuracaoAppSettings.GetConnectionString("ConexaoPadrao");
        optionsBuilder.UseSqlServer(connectionString);
    }
}

