using ElectronicService.Domain.Users;

namespace ElectronicService.Core.Abstractions;

public interface IJwtTokenProvider
{
    string CreateToken(User user);
}