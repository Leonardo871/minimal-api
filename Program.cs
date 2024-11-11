using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using MinimalAPI.Infraestrutura.Db;
using MinimalAPI.Dominio.interfaces;
using MinimalAPI.Dominio.Entidades;
using MinimalAPI.Dominio.Servicos;
using Microsoft.AspNetCore.Mvc;
using MinimalAPI.Dominio.ModelViews;
using MinimalAPI.DTOs;
using MinimalAPI.Dominio.Enuns;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;

# region Builder
var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if(!string.IsNullOrEmpty(key)) key = "123456";

builder.Services.AddAuthentication(option =>  {
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(option => {
    option.TokenValidationParameters = new TokenValidationParameters{
        ValidateLifetime = true,   
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false,
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme{
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta maneira : Bearar {Seu token}"
    });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme{
                    Reference = new OpenApiReference 
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
    
});

builder.Services.AddDbContext<DbContexto>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexaoPadrao")));

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

#endregion

# region Home
app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
# endregion

# region Administradores

string GerarTokenJwt(Administrador administrador){
    if(string.IsNullOrEmpty(key)) return string.Empty;
    
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>()
    {
        new Claim("Email", administrador.Email),
        new Claim("Perfil", administrador.Perfil),
        new Claim(ClaimTypes.Role,administrador.Perfil),
    };

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddDays(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) => {
    var adm = administradorServico.Login(loginDTO);
    if(adm != null)
    {
        string token = GerarTokenJwt(adm);
        return Results.Ok(new AdministradorLogado
        {
            Email = adm.Email,
            Perfil = adm.Perfil,
            Token = token
        });
    }
    else
        return Results.Unauthorized();
}).AllowAnonymous().WithTags("Administradores");

app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) => {
        var adms = new List<AdministradorModelView>{};
        var administradores = administradorServico.Todos(pagina);
        foreach(var adm in administradores)
        {
            adms.Add(new  AdministradorModelView{
                Id = adm.Id,
                Email = adm.Email,
                Perfil = adm.Perfil
            }
           );
        }
        return Results.Ok(adms);
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Administradores");

app.MapGet("/Administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) => {
    var administrador = administradorServico.BuscaPorId(id);

    if(administrador == null ) return Results.NotFound();

    return Results.Ok(new  AdministradorModelView{
                Id = administrador.Id,
                Email = administrador.Email,
                Perfil = administrador.Perfil
                
        });

}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Administradores");

app.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) => {
    var validacao = new ErrosDeValidacao{
        Mensagem = new List<string>()
    };

    if(string.IsNullOrEmpty(administradorDTO.Email))
        validacao.Mensagem.Add("Email não pode ser vazio");
    if(string.IsNullOrEmpty(administradorDTO.Senha))
        validacao.Mensagem.Add("Senha não pode ser vazia");
    if(administradorDTO.Perfil == null)
        validacao.Mensagem.Add("Perfil não pode ser vazio");

    if(validacao.Mensagem.Count > 0)
        return Results.BadRequest(validacao);

        var administrador = new Administrador{
            Email = administradorDTO.Email,
            Senha = administradorDTO.Senha,
            Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
        };
          
    
    administradorServico.Incluir(administrador); 

    return Results.Created($"/administrador/{administrador.Id}", new  AdministradorModelView{
            Id = administrador.Id,
            Email = administrador.Email,
            Perfil = administrador.Perfil
    });

})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Administradores");

#endregion

#region Veiculos

ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{
    var validacao = new ErrosDeValidacao(){
        Mensagem = new List<string>()
 };

    if(string.IsNullOrEmpty(veiculoDTO.Nome))
        validacao.Mensagem.Add("O Nome não pode ser vazio");
    
    if(string.IsNullOrEmpty(veiculoDTO.Marca))
        validacao.Mensagem.Add("A marca não pode ser vazio");

    if(veiculoDTO.Ano < 1950)
        validacao.Mensagem.Add("Veiculo muito antigo!");

    return validacao;
}

app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => {

    var validacao = validaDTO(veiculoDTO);

    if(validacao.Mensagem.Count > 0)
        return Results.BadRequest(validacao);

    var veiculo = new Veiculo{
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano
    };
    veiculoServico.Incluir(veiculo);

    return Results.Created($"/veiculo{veiculo.Id}",veiculo);
   
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm,Editor"})
.WithTags("Veiculos");

app.MapGet("/veiculos", ([FromQuery] int? pagina , IVeiculoServico veiculoServico) => {
    var veiculos = veiculoServico.Todos(pagina);

    return Results.Ok(veiculos);
   
}).RequireAuthorization().WithTags("Veiculos");

app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) => {
    var veiculo = veiculoServico.BuscaPorId(id);

    if(veiculo == null ) return Results.NotFound();

    return Results.Ok(veiculo);
   
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm,Editor"})
.WithTags("Veiculos");

app.MapPut("/veiculos/{id}", ([FromRoute] int id,VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => {

    var veiculo = veiculoServico.BuscaPorId(id);
    if(veiculo == null ) return Results.NotFound();

    var validacao = validaDTO(veiculoDTO);
    if(validacao.Mensagem.Count > 0)
        return Results.BadRequest(validacao);

    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    veiculoServico.Atualizar(veiculo);

    return Results.Ok(veiculo);
   
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Veiculos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) => {
    var veiculo = veiculoServico.BuscaPorId(id);
    if(veiculo == null ) return Results.NotFound();

    veiculoServico.Apagar(veiculo);

    return Results.NoContent();
   
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Veiculos");
#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion
