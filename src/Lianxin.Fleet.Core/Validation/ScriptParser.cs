using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Validation;

// 脚本按行书写，每行一个命令，# 或 // 开头为注释。坐标是 0–1 的相对位置，时间单位为秒。
// 「重复」「如果」「当」开始一个块，以「结束」收尾，最多嵌套 MaxDepth 层；Parse 返回顶层语句，块内语句在 Body/ElseBody 中。
public static partial class ScriptParser
{
    public const int MaxSourceLength = 50_000;
    public const int MaxSteps = 2000;
    public const int MaxDepth = 8;
    public const int MaxRepeat = 100_000;

    private sealed class Block(ScriptStep? header, string keyword)
    {
        public ScriptStep? Header { get; } = header;
        public string Keyword { get; } = keyword;
        public List<ScriptStep> Body { get; } = [];
        public List<ScriptStep>? ElseBody
        {
            get; set;
        }
        public bool IsLoop => Header?.Kind is ScriptStepKind.Repeat or ScriptStepKind.While;
        public void Add(ScriptStep step) => (ElseBody ?? Body).Add(step);
        public ScriptStep Close() => Header! with { Body = Body.ToArray(), ElseBody = ElseBody?.ToArray() ?? [] };
    }

    public static IReadOnlyList<ScriptStep> Parse(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("脚本内容不能为空。");
        if (source.Length > MaxSourceLength)
            throw new ArgumentException($"脚本最多 {MaxSourceLength} 个字符。");
        var root = new Block(null, "");
        var blocks = new Stack<Block>();
        blocks.Push(root);
        var count = 0;
        var lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("//", StringComparison.Ordinal))
                continue;
            try
            {
                Statement(index + 1, line, blocks);
            }
            catch (Exception error) when (error is ArgumentException or FormatException)
            {
                throw new ArgumentException($"第 {index + 1} 行：{error.Message}", error);
            }
            if (++count > MaxSteps)
                throw new ArgumentException($"脚本最多 {MaxSteps} 步。");
        }
        if (blocks.Count > 1)
        {
            var open = blocks.Peek();
            throw new ArgumentException($"第 {open.Header!.Line} 行的「{open.Keyword}」缺少对应的「结束」。");
        }
        if (root.Body.Count == 0)
            throw new ArgumentException("脚本没有可执行的命令。");
        return root.Body.ToArray();
    }

    // 全部语句数（含块内语句与块本身），用于编辑器提示。
    public static int CountSteps(IReadOnlyList<ScriptStep> steps) =>
        steps.Sum(step => 1 + CountSteps(step.Body) + CountSteps(step.ElseBody));

    private static void Statement(int number, string line, Stack<Block> blocks)
    {
        var space = line.IndexOfAny([' ', '\t']);
        var word = space < 0 ? line : line[..space];
        var rest = space < 0 ? "" : line[(space + 1)..].Trim();
        var command = Aliases.TryGetValue(word, out var alias) ? alias : word.ToLowerInvariant();
        var label = line.Length > 40 ? line[..40] + "…" : line;
        var current = blocks.Peek();
        switch (command)
        {
            case "repeat" or "if" or "while":
                if (blocks.Count > MaxDepth)
                    throw new ArgumentException($"块最多嵌套 {MaxDepth} 层。");
                var header = new ScriptStep(number, label, null, new());
                header = command switch
                {
                    "repeat" => header with
                    {
                        Kind = ScriptStepKind.Repeat,
                        Count = Integer(rest, 1, MaxRepeat, "重复次数")
                    },
                    "if" => header with
                    {
                        Kind = ScriptStepKind.If,
                        Condition = Condition(rest)
                    },
                    _ => header with
                    {
                        Kind = ScriptStepKind.While,
                        Condition = Condition(rest)
                    }
                };
                blocks.Push(new Block(header, word));
                return;
            case "else":
                Nothing(word, rest);
                if (current.Header?.Kind != ScriptStepKind.If)
                    throw new ArgumentException("「否则」前面没有对应的「如果」。");
                if (current.ElseBody is not null)
                    throw new ArgumentException("一个「如果」只能有一个「否则」。");
                current.ElseBody = [];
                return;
            case "end":
                Nothing(word, rest);
                if (current.Header is null)
                    throw new ArgumentException("多余的「结束」，前面没有需要结束的「重复」「如果」或「当」。");
                blocks.Pop();
                blocks.Peek().Add(current.Close());
                return;
            case "break":
                Nothing(word, rest);
                if (!blocks.Any(block => block.IsLoop))
                    throw new ArgumentException("「跳出」只能写在「重复」或「当」循环里。");
                current.Add(new ScriptStep(number, label, null, new()) { Kind = ScriptStepKind.Break });
                return;
            default:
                current.Add(Command(number, label, command, word, rest));
                return;
        }
    }

    private static void Nothing(string word, string rest)
    {
        if (rest.Length > 0)
            throw new ArgumentException($"「{word}」后面不能有其他内容。");
    }
}
