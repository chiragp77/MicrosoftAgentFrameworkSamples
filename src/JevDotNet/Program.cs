using JevDotNet;
using JevDotNet.Models;
using Shared;

Utils.Init("JevDotNet NuGet Demo");

string apiKey = SecretsManager.GetSecrets().TypeSafeApiKey;

JevClient client = new JevClient(apiKey);

JevResponse<MyJevReturnObject> response = await client.EvaluateAsync<MyJevReturnObject>("The device is broken. in need support");

Console.WriteLine("");

public class MyJevReturnObject
{
    [JevChoiceQuestion<Category>("What type of category fits this message?")]
    public JevChoice<Category>? Category { get; set; }

    [JevScoreQuestion<Severity>("How bad is it?")]
    public JevScore<Severity> Severity { get; set; }

    [JevNoulQuestion("Is the product broken?")]
    public JevNoul? IsBroken { get; set; }
}

public enum Severity
{
    ALittle,
    Somewhat,
    ALot
}

public enum Category
{
    Support,
    Billing,
    Other
}