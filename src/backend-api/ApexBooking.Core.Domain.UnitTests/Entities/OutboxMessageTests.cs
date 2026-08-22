using System;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using Xunit;

namespace ApexBooking.Core.Domain.UnitTests.Entities;

public class OutboxMessageTests
{
    private static OutboxMessage CreatePendingMessage() =>
        OutboxMessage.Create("SomeEvent", "{}", DateTime.UtcNow);

    [Fact]
    public void Create_StartsPendingWithNoRetriesAndNoBackoff()
    {
        var message = CreatePendingMessage();

        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.NextAttemptAtUtc);
    }

    [Theory]
    [InlineData(1, 1)] // 2^(1-1) = 1 minute
    [InlineData(2, 2)] // 2^(2-1) = 2 minutes
    [InlineData(3, 4)] // 2^(3-1) = 4 minutes
    [InlineData(4, 8)] // 2^(4-1) = 8 minutes
    public void MarkFailed_BelowMaxRetryCount_RevertsToPendingWithExponentialBackoff(int attemptNumber, int expectedDelayMinutes)
    {
        var message = CreatePendingMessage();
        var before = DateTime.UtcNow;

        for (var i = 0; i < attemptNumber; i++)
            message.MarkFailed("transient error");

        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(attemptNumber, message.RetryCount);
        Assert.NotNull(message.NextAttemptAtUtc);
        Assert.InRange(
            message.NextAttemptAtUtc!.Value,
            before.AddMinutes(expectedDelayMinutes).AddSeconds(-5),
            before.AddMinutes(expectedDelayMinutes).AddSeconds(5));
    }

    [Fact]
    public void MarkFailed_AtMaxRetryCount_BecomesTerminallyFailedWithNoBackoff()
    {
        var message = CreatePendingMessage();

        for (var i = 0; i < OutboxMessage.MaxRetryCount; i++)
            message.MarkFailed("transient error");

        Assert.Equal(OutboxMessageStatus.Failed, message.Status);
        Assert.Equal(OutboxMessage.MaxRetryCount, message.RetryCount);
        Assert.Null(message.NextAttemptAtUtc);
    }

    [Fact]
    public void MarkFailedPermanently_SkipsRetryBudgetAndFailsImmediately()
    {
        var message = CreatePendingMessage();

        message.MarkFailedPermanently("malformed recipient");

        Assert.Equal(OutboxMessageStatus.Failed, message.Status);
        Assert.Equal(1, message.RetryCount); // one attempt was made, just never eligible for more
        Assert.Null(message.NextAttemptAtUtc);
    }

    [Fact]
    public void MarkProcessed_ClearsBackoffAndError()
    {
        var message = CreatePendingMessage();
        message.MarkFailed("transient error");

        message.MarkProcessed();

        Assert.Equal(OutboxMessageStatus.Processed, message.Status);
        Assert.Null(message.NextAttemptAtUtc);
        Assert.Null(message.LastError);
    }

    [Fact]
    public void TryRetry_FromFailed_ResetsRetryBudgetAndBackoff()
    {
        var message = CreatePendingMessage();
        for (var i = 0; i < OutboxMessage.MaxRetryCount; i++)
            message.MarkFailed("transient error");

        var result = message.TryRetry();

        Assert.True(result);
        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.NextAttemptAtUtc);
    }

    [Fact]
    public void TryRetry_WhenNotFailed_ReturnsFalseAndLeavesStateUnchanged()
    {
        var message = CreatePendingMessage();

        var result = message.TryRetry();

        Assert.False(result);
        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
    }
}
