using System.Text;
using Anthropic.Models.Beta.Messages;

namespace Lianxin.Fleet.Core.Agents;

internal sealed record AgentReply(List<BetaContentBlockParam> Blocks, List<BetaToolUseBlock> ToolUses, string Text);
internal sealed record ToolOutcome(string ToolUseId, string Text, bool IsError);

// 对话记录只追加不改写：模型回复按原样（含思考块签名）回传，工具结果放在下一条用户消息里。
internal static class AgentTranscript
{
    public static AgentReply Echo(BetaMessage response, IProgress<AgentStep> progress)
    {
        var blocks = new List<BetaContentBlockParam>();
        var uses = new List<BetaToolUseBlock>();
        var text = new StringBuilder();
        foreach (var block in response.Content)
        {
            if (block.TryPickThinking(out var thinking))
            {
                blocks.Add(new BetaThinkingBlockParam { Thinking = thinking.Thinking, Signature = thinking.Signature });
                if (!string.IsNullOrWhiteSpace(thinking.Thinking))
                    progress.Report(new("思考", thinking.Thinking.Trim()));
            }
            else if (block.TryPickRedactedThinking(out var redacted))
                blocks.Add(new BetaRedactedThinkingBlockParam { Data = redacted.Data });
            else if (block.TryPickText(out var said))
            {
                blocks.Add(new BetaTextBlockParam { Text = said.Text });
                text.Append(said.Text);
                if (!string.IsNullOrWhiteSpace(said.Text))
                    progress.Report(new("说明", said.Text.Trim()));
            }
            else if (block.TryPickToolUse(out var use))
            {
                blocks.Add(new BetaToolUseBlockParam { ID = use.ID, Name = use.Name, Input = use.Input });
                uses.Add(use);
            }
        }
        return new(blocks, uses, text.ToString().Trim());
    }

    // 每个工具调用都要有对应结果；新截图只附在最后一个结果上，避免重复发送图片。
    public static List<BetaContentBlockParam> Results(IReadOnlyList<ToolOutcome> outcomes, BetaImageBlockParam screenshot,
        AgentScreen screen)
    {
        var blocks = new List<BetaContentBlockParam>();
        for (var index = 0; index < outcomes.Count; index++)
        {
            var outcome = outcomes[index];
            var result = new BetaToolResultBlockParam { ToolUseID = outcome.ToolUseId, IsError = outcome.IsError, Content = outcome.Text };
            if (index == outcomes.Count - 1)
                result = new BetaToolResultBlockParam
                {
                    ToolUseID = outcome.ToolUseId,
                    IsError = outcome.IsError,
                    Content = new List<Block>
                    {
                        new BetaTextBlockParam { Text = $"{outcome.Text} New screenshot ({screen.Width}x{screen.Height} pixels):" },
                        screenshot,
                    },
                };
            blocks.Add(result);
        }
        return blocks;
    }
}
