using System.ComponentModel;

namespace JevClassification.Models;

public sealed class ClassificationBatch
{
    [Description("One classification per input message, in the same order as the input.")]
    public required List<MessageClassification> Classifications { get; init; }
}

public sealed class MessageClassification
{
    [Description("The input message ID, copied exactly.")]
    public required string MessageId { get; init; }

    [Description("The sentiment category.")]
    public SentimentLevel Sentiment { get; init; }

    [Description("Sentiment from -100 (very negative), through 0 (neutral), to 100 (very positive).")]
    public double SentimentScore { get; init; }

    [Description("The form of the message.")]
    public MessageType MessageType { get; init; }

    [Description("The main subject of the message.")]
    public MessageTopic Topic { get; init; }

    [Description("Whether the message needs urgent attention.")]
    public bool RequiresUrgentAttention { get; init; }
}

public enum SentimentLevel
{
    VeryNegative,
    Negative,
    Neutral,
    Positive,
    VeryPositive
}

public enum MessageType
{
    Review,
    Comment,
    Question,
    SupportRequest,
    Other
}

public enum MessageTopic
{
    ProductQuality,
    Shipping,
    Billing,
    FeatureRequest,
    CustomerService,
    Other
}
