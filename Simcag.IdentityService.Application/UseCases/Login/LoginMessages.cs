namespace Simcag.IdentityService.Application.UseCases.Login;

public static class LoginMessages
{
    public const string InvalidCredentials = "Email ou senha inválidos";

    public const string WrongTenant =
        "Seu usuário não está vinculado ao condomínio selecionado. Verifique a opção escolhida e tente novamente.";
}
