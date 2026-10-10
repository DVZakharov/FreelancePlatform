using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Messages.GetContractMessages;
using FreelancePlatform.Application.Features.Messages.SendMessage;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Tests.Unit.Messages;

public class MessagesTests
{
    private readonly TestDb _testDb = new();

    private Task<IResult> SendAsync(Guid contractId, Guid actorId, string content) =>
        _testDb.RunAsync(db => SendMessageEndpoint.Handle(
            contractId.ToString(), new SendMessageRequest(content),
            TestHttp.As(actorId), new SendMessageValidator(), db, TestContext.Current.CancellationToken));

    [Fact(Timeout = 10_000)]
    public async Task Send_ByParticipant_SavesMessage()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100);

        var response = await SendAsync(s.ContractId, s.FreelancerId, "Привет, начинаю работу");

        Assert.Equal(201, ResultAssert.Unwrap<MessageDto>(response).Code);

        await using var check = _testDb.NewContext();
        var message = await check.Messages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(s.FreelancerId, message.SenderId);
        Assert.Equal(s.ContractId, message.ContractId);
    }

    [Fact]
    public async Task Send_ByStranger_Returns403_AndSavesNothing()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100);

        var response = await SendAsync(s.ContractId, Guid.NewGuid(), "Я тут случайно");

        Assert.Equal(403, ResultAssert.Unwrap<MessageDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Empty(check.Messages);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Send_EmptyContent_Returns400(string content)
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100);

        Assert.Equal(400, ResultAssert.Unwrap<MessageDto>(await SendAsync(s.ContractId, s.ClientId, content)).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Get_ReturnsOnlyThisContractsMessages_NewestFirst_AndRejectsStranger()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100);
        var other = await Seed.MilestoneAsync(_testDb, 0, 100);

        await using (var db = _testDb.NewContext())
        {
            db.Messages.AddRange(
                new MessageEntity { ContractId = s.ContractId, SenderId = s.ClientId, Content = "first", CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new MessageEntity { ContractId = s.ContractId, SenderId = s.FreelancerId, Content = "second", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new MessageEntity { ContractId = other.ContractId, SenderId = other.ClientId, Content = "foreign" });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var ok = await _testDb.RunAsync(db => GetContractMessagesEndpoint.Handle(
             s.ContractId.ToString(), new GetContractMessagesRequest {PageNumber = 1, PageSize = 50 },
             new GetContractMessagesValidator(),TestHttp.As(s.ClientId),  db, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<GetContractMessagesResponse>(ok);
        Assert.Equal(["second", "first"], result.Value!.Items.Select(m => m.Content));
        Assert.Equal(2, result.Value.TotalCount);

        var denied = await _testDb.RunAsync(db => GetContractMessagesEndpoint.Handle(
             s.ContractId.ToString(), new GetContractMessagesRequest {PageNumber = 1, PageSize = 50 },
             new GetContractMessagesValidator(),TestHttp.As(other.ClientId),  db, TestContext.Current.CancellationToken));

        Assert.Equal(403, ResultAssert.Unwrap<GetContractMessagesResponse>(denied).Code);
    }
}