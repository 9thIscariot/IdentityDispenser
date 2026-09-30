using System;

public sealed class IdentityData
{
    public string Id { get; }
    public string SinnerName { get; }
    public string Name { get; }
    public string DisplayName => $"{Name} {SinnerName}";

    public IdentityData(string id, string sinnerName, string name)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("인격 ID가 필요합니다.", nameof(id));
        if (string.IsNullOrWhiteSpace(sinnerName))
            throw new ArgumentException("수감자 이름이 필요합니다.", nameof(sinnerName));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("인격 이름이 필요합니다.", nameof(name));

        Id = id;
        SinnerName = sinnerName;
        Name = name;
    }
}
