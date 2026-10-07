using AeroLicense.Application.Abstractions;
using AeroLicense.Domain.Entities;
using AeroLicense.Domain.Enums;

namespace AeroLicense.Tests.TestHelpers;

public sealed class FakeCurrentUser(Guid userId, UserRole role, Guid? organizationId = null) : ICurrentUser
{
    public Guid UserId { get; } = userId;
    public UserRole Role { get; } = role;
    public Guid? OrganizationId { get; } = organizationId;

    public static FakeCurrentUser From(User user) => new(user.Id, user.Role, user.OrganizationId);
}
