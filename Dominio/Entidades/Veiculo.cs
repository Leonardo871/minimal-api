using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MinimalAPI.Dominio.Entidades;


public class Veiculo 
{
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = default!;
        [Required]
        [StringLength(150)]
        public string Marca { get; set; } = default!;
        [Required]
        public string Nome { get; set; } = default!;
        [Required]
        [StringLength(100)]
        public int Ano { get; set; } = default!;
        
}   