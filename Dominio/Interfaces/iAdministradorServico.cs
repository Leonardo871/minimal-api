using MinimalAPI.Dominio.Entidades;
using MinimalAPI.DTOs;

namespace MinimalAPI.Dominio.interfaces;


public interface IAdministradorServico
{
    Administrador? Login(LoginDTO loginDTO);
    Administrador? Incluir(Administrador administrador);
    List<Administrador> Todos(int? pagina);

    Administrador? BuscaPorId(int id );


}