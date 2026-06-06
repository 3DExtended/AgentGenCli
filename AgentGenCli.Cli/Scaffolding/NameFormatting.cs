namespace AgentGenCli.Cli.Scaffolding;

internal static class NameFormatting
{
    public static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = new List<char>(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && value[i - 1] != '_' && !char.IsUpper(value[i - 1]))
                {
                    chars.Add('_');
                }

                chars.Add(char.ToLowerInvariant(c));
            }
            else if (c == '-' || c == ' ')
            {
                chars.Add('_');
            }
            else
            {
                chars.Add(char.ToLowerInvariant(c));
            }
        }

        return new string(chars.ToArray());
    }
}
