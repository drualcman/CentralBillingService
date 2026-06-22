using System.ComponentModel.DataAnnotations;

namespace CentralBillingService.VerifyUI.Models;

public sealed class ContactForm
{
    [Required(ErrorMessage = "Indica tu nombre.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu mensaje.")]
    [MinLength(10, ErrorMessage = "El mensaje debe tener al menos 10 caracteres.")]
    public string Message { get; set; } = string.Empty;
}
