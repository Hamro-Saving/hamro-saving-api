using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Members.Create;
using HamroSavings.Application.Members.Get;
using HamroSavings.Application.Members.GetById;
using HamroSavings.Domain.Members;

namespace HamroSavings.Api.Endpoints.Members;

public sealed class CreateMember : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("members", async (
            CreateMemberRequest request,
            ICommandHandler<CreateMemberCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var memberType = Enum.Parse<MembershipType>(request.MembershipType);
            var command = new CreateMemberCommand(
                memberType,
                request.FirstName,
                request.LastName,
                request.Email,
                request.PhoneNumber,
                request.Address,
                request.GroupId);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/members/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("Members")
        .RequireAuthorization()
        .WithSummary("Create a new member or non-member");
    }
}

public sealed class GetMembers : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("members", async (
            Guid? groupId,
            bool? includeAdmins,
            string? membershipType,
            IQueryHandler<GetMembersQuery, List<MemberResponse>> handler,
            CancellationToken ct) =>
        {
            MembershipType? parsedMemberType = membershipType != null ? Enum.Parse<MembershipType>(membershipType) : null;
            var result = await handler.Handle(new GetMembersQuery(groupId, includeAdmins ?? false, parsedMemberType), ct);
            return result.Match(
                members => Results.Ok(members),
                error => CustomResults.Problem(error));
        })
        .WithTags("Members")
        .RequireAuthorization()
        .WithSummary("Get all members");
    }
}

public sealed class GetMemberById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("members/{id:guid}", async (
            Guid id,
            IQueryHandler<GetMemberByIdQuery, MemberResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetMemberByIdQuery(id), ct);
            return result.Match(
                member => Results.Ok(member),
                error => CustomResults.Problem(error));
        })
        .WithTags("Members")
        .RequireAuthorization()
        .WithSummary("Get member by ID");
    }
}

public sealed record CreateMemberRequest(
    string MembershipType,
    string FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? Address,
    Guid GroupId);
