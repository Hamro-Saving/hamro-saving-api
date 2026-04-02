using HamroSavings.Domain.Users;

namespace HamroSavings.Application.Abstractions.Authentication;

public interface ITokenProvider
{
    string Create(User user);
}
