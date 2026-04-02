using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Extensions;
using HamroSavings.Api.Infrastructure;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.NonMembers.Create;
using HamroSavings.Application.NonMembers.Get;
using HamroSavings.Application.NonMembers.GetById;

namespace HamroSavings.Api.Endpoints.NonMembers;

public sealed class CreateNonMember : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("non-members", async (
            CreateNonMemberRequest request,
            ICommandHandler<CreateNonMemberCommand, Guid> handler,
            CancellationToken ct) =>
        {
            var command = new CreateNonMemberCommand(
                request.FullName,
                request.Email,
                request.Phone,
                request.Address,
                request.GroupId);

            var result = await handler.Handle(command, ct);
            return result.Match(
                id => Results.Created($"/api/v1/non-members/{id}", new { Id = id }),
                error => CustomResults.Problem(error));
        })
        .WithTags("NonMembers")
        .RequireAuthorization()
        .WithSummary("Create a new non-member");
    }
}

public sealed class GetNonMembers : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("non-members", async (
            Guid? groupId,
            IQueryHandler<GetNonMembersQuery, List<NonMemberResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetNonMembersQuery(groupId), ct);
            return result.Match(
                nonMembers => Results.Ok(nonMembers),
                error => CustomResults.Problem(error));
        })
        .WithTags("NonMembers")
        .RequireAuthorization()
        .WithSummary("Get all non-members");
    }
}

public sealed class GetNonMemberById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("non-members/{id:guid}", async (
            Guid id,
            IQueryHandler<GetNonMemberByIdQuery, NonMemberResponse> handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetNonMemberByIdQuery(id), ct);
            return result.Match(
                nonMember => Results.Ok(nonMember),
                error => CustomResults.Problem(error));
        })
        .WithTags("NonMembers")
        .RequireAuthorization()
        .WithSummary("Get non-member by ID");
    }
}

public sealed record CreateNonMemberRequest(
    string FullName,
    string? Email,
    string? Phone,
    string? Address,
    Guid GroupId);
