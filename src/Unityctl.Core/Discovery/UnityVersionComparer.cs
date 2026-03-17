namespace Unityctl.Core.Discovery;

public sealed class UnityVersionComparer : IComparer<string?>
{
    public static UnityVersionComparer Instance { get; } = new();

    public int Compare(string? left, string? right)
    {
        if (ReferenceEquals(left, right))
            return 0;
        if (left is null)
            return -1;
        if (right is null)
            return 1;

        var leftNumbers = ExtractNumbers(left);
        var rightNumbers = ExtractNumbers(right);
        var maxLength = Math.Max(leftNumbers.Count, rightNumbers.Count);

        for (var i = 0; i < maxLength; i++)
        {
            var leftValue = i < leftNumbers.Count ? leftNumbers[i] : 0;
            var rightValue = i < rightNumbers.Count ? rightNumbers[i] : 0;
            if (leftValue != rightValue)
                return leftValue.CompareTo(rightValue);
        }

        var streamComparison = GetStreamRank(left).CompareTo(GetStreamRank(right));
        return streamComparison != 0
            ? streamComparison
            : StringComparer.OrdinalIgnoreCase.Compare(left, right);
    }

    private static List<int> ExtractNumbers(string version)
    {
        var numbers = new List<int>();
        var current = 0;
        var hasDigits = false;

        foreach (var character in version)
        {
            if (char.IsDigit(character))
            {
                current = (current * 10) + (character - '0');
                hasDigits = true;
                continue;
            }

            if (!hasDigits)
                continue;

            numbers.Add(current);
            current = 0;
            hasDigits = false;
        }

        if (hasDigits)
            numbers.Add(current);

        return numbers;
    }

    private static int GetStreamRank(string version)
    {
        foreach (var character in version)
        {
            if (!char.IsLetter(character))
                continue;

            return char.ToLowerInvariant(character) switch
            {
                'a' => 0,
                'b' => 1,
                'c' => 2,
                'f' => 3,
                'p' => 4,
                _ => 5
            };
        }

        return 6;
    }
}
