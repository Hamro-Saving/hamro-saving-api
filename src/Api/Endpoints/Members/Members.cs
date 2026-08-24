using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Members.Create;
using HamroSavings.Application.Members.Get;
using HamroSavings.Application.Members.GetById;
using HamroSavings.Domain.Members;
using Microsoft.AspNetCore.Mvc;

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
            var command = new CreateMemberCommand(
                Enum.Parse<GroupRole>(request.GroupRole),
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
        .WithSummary("Add someone to a group (group admin or SuperAdmin)");
    }
}

public sealed class GetMembers : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("members", async (
            Guid? groupId,
            [FromQuery] string[]? roles,
            IQueryHandler<GetMembersQuery, List<MemberResponse>> handler,
            CancellationToken ct) =>
        {
            var parsedRoles = roles?.Select(Enum.Parse<GroupRole>).ToList();
            var result = await handler.Handle(new GetMembersQuery(groupId, parsedRoles), ct);
            return result.Match(
                members => Results.Ok(members),
                error => CustomResults.Problem(error));
        })
        .WithTags("Members")
        .RequireAuthorization(Policies.GroupRead)
        .WithSummary("Get members, optionally filtered by group role");
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
        .RequireAuthorization(Policies.GroupRead)
        .WithSummary("Get member by ID");
    }
}

public sealed record CreateMemberRequest(
    string GroupRole,
    string FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? Address,
    Guid? GroupId);
