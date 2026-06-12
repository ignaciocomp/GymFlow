using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;

namespace GymFlow.Application.UseCases.Auth;

public class LoginConGoogleCommand
{
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly ISocioRepository _socioRepository;

    public LoginConGoogleCommand(IGoogleTokenValidator googleTokenValidator, ISocioRepository socioRepository)
    {
        _googleTokenValidator = googleTokenValidator;
        _socioRepository = socioRepository;
    }

    public async Task<Socio> ExecuteAsync(string idToken)
    {
        var payload = await _googleTokenValidator.ValidarAsync(idToken);
        if (payload is null || !payload.EmailVerificado)
            throw new UnauthorizedAccessException("Token de Google inválido.");

        var socio = await _socioRepository.GetByCorreoAsync(payload.Email);
        if (socio is null || !socio.EstaActivo || !socio.RolId.HasValue)
            throw new UnauthorizedAccessException("No encontramos una cuenta asociada a este correo.");

        if (string.IsNullOrWhiteSpace(socio.GoogleUserId))
        {
            socio.VincularGoogle(payload.Sub);
            await _socioRepository.SaveChangesAsync();
        }

        return socio;
    }
}
