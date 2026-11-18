using System.Text.Json;
using Lianxin.Sms.Checks.Infrastructure;

namespace Lianxin.Sms.Checks.Scenarios;

internal static class GatewayChecks
{
    public static async Task Run()
    {
        await using var fixture = await HttpFixture.Create(false);
        var admin = await fixture.Login("admin@lianxin.local");
        await admin.Call("/admin/accounts", new
        {
            email = "alice@example.test",
            name = "Alice",
            tenant = "甲租户",
            password = HttpFixture.Password,
            quota = 3
        }, 201);
        await admin.Call("/admin/accounts", new
        {
            email = "bob@example.test",
            name = "Bob",
            tenant = "乙租户",
            password = HttpFixture.Password,
            quota = 1
        }, 201);
        var alice = await fixture.Login("alice@example.test");
        var bob = await fixture.Login("bob@example.test");
        const string recipient = "+12025550191";
        var outgoing = new
        {
            key = "a",
            recipients = new[] { recipient },
            body = "Alice 发出"
        };
        await alice.Call("/messages", outgoing, 409);
        Assert.Equal(3, (await alice.Call("/me")).GetProperty("quota").GetInt32());
        var a = await admin.Call("/admin/devices", new
        {
            name = "iPhone A",
            line = "+12025550101"
        }, 201);
        await admin.Call("/admin/devices", new
        {
            name = "重复登记",
            line = "+1 (202) 555-0101"
        }, 409);
        var b = await admin.Call("/admin/devices", new
        {
            name = "iPhone B",
            line = "+12025550102"
        }, 201);
        var deviceA = fixture.Client(a.GetProperty("token").GetString());
        var deviceB = fixture.Client(b.GetProperty("token").GetString());
        await fixture.Client().Call("/device/poll", new
        {
            canSend = true
        }, 401);
        await deviceA.Call("/device/poll", new
        {
            canSend = false
        });
        await alice.Call("/messages", outgoing, 409);
        await deviceA.Call("/device/poll", new
        {
            canSend = true
        });
        await deviceB.Call("/device/poll", new
        {
            canSend = true
        });
        var sentA = await alice.Call("/messages", outgoing, 201);
        var sentB = await bob.Call("/messages", new
        {
            key = "b",
            recipients = new[] { recipient, recipient },
            body = "Bob 发出"
        }, 201);
        Assert.Equal(0, (await bob.Call("/me")).GetProperty("quota").GetInt32());
        await bob.Call("/messages", new
        {
            key = "insufficient",
            recipients = new[] { recipient },
            body = "额度不足"
        }, 409);
        var jobA = (await deviceA.Call("/device/poll", new
        {
            canSend = true
        })).GetProperty("job");
        var jobB = (await deviceB.Call("/device/poll", new
        {
            canSend = true
        })).GetProperty("job");
        Assert.Equal("Alice 发出", jobA.GetProperty("body").GetString());
        Assert.Equal("Bob 发出", jobB.GetProperty("body").GetString());
        await deviceB.Call("/device/claim", new
        {
            id = jobA.GetProperty("id").GetInt64()
        }, 409);
        await deviceA.Call("/device/claim", new
        {
            id = jobA.GetProperty("id").GetInt64()
        });
        await deviceA.Call("/device/claim", new
        {
            id = jobA.GetProperty("id").GetInt64()
        }, 409);
        Assert.Equal(JsonValueKind.Null, (await deviceA.Call("/device/poll", new
        {
            canSend = true
        })).GetProperty("job").ValueKind);
        await deviceA.Call("/device/report", new
        {
            id = jobA.GetProperty("id").GetInt64(),
            status = "submitted"
        });
        await deviceB.Call("/device/claim", new
        {
            id = jobB.GetProperty("id").GetInt64()
        });
        var failed = new
        {
            id = jobB.GetProperty("id").GetInt64(),
            status = "failed"
        };
        await Task.WhenAll(deviceB.Call("/device/report", failed), deviceB.Call("/device/report", failed));
        Assert.Equal(1, (await bob.Call("/me")).GetProperty("quota").GetInt32());
        await deviceB.Call("/device/report", new
        {
            id = jobB.GetProperty("id").GetInt64(),
            status = "submitted"
        }, 409);
        var incoming = new
        {
            from = recipient,
            @event = "same-carrier-id",
            body = "仅 Alice 可见"
        };
        var inbound = await deviceA.Call("/device/inbound", incoming);
        Assert.Equal(inbound.GetProperty("id").GetInt64(), (await deviceA.Call("/device/inbound", incoming)).GetProperty("id").GetInt64());
        await deviceA.Call("/device/inbound", new
        {
            from = recipient,
            @event = "same-carrier-id",
            body = "冲突正文"
        }, 409);
        await deviceB.Call("/device/inbound", new
        {
            from = recipient,
            @event = "same-carrier-id",
            body = "仅 Bob 可见"
        });
        await deviceA.Call("/device/inbound", new
        {
            from = "+12025550188",
            @event = "unmatched",
            body = "无归属"
        }, 404);
        var conversationA = sentA.GetProperty("conversation").GetInt64();
        var conversationB = sentB.GetProperty("conversation").GetInt64();
        await bob.Call("/conversations/" + conversationA, expected: 404);
        await alice.Call("/conversations/" + conversationB, expected: 404);
        Assert.Equal("Alice 发出|仅 Alice 可见", Bodies(await alice.Call("/conversations/" + conversationA)));
        Assert.Equal("Bob 发出|仅 Bob 可见", Bodies(await bob.Call("/conversations/" + conversationB)));
        await alice.Call($"/conversations/{conversationA}/read", new
        {
            through = inbound.GetProperty("id").GetInt64()
        });
        Assert.Equal(0, (await alice.Call("/inbox")).GetProperty("stats").GetProperty("unread").GetInt32());
        var outcomes = await Task.WhenAll(Enumerable.Range(1, 3).Select(async i =>
        {
            try
            {
                await alice.Call("/messages", new
                {
                    key = "reply-" + i,
                    conversation = conversationA,
                    body = "从原设备回复"
                }, 201);
                return true;
            }
            catch (InvalidOperationException error) when (error.Message.Contains("got 409:", StringComparison.Ordinal)) { return false; }
        }));
        Assert.Equal(2, outcomes.Count(success => success));
        Assert.Equal("从原设备回复", (await deviceA.Call("/device/poll", new
        {
            canSend = true
        })).GetProperty("job").GetProperty("body").GetString());
        Assert.Equal(JsonValueKind.Null, (await deviceB.Call("/device/poll", new
        {
            canSend = true
        })).GetProperty("job").ValueKind);
        Assert.Equal(0, (await alice.Call("/me")).GetProperty("quota").GetInt32());
        var aliceId = (await alice.Call("/me")).GetProperty("id").GetInt64();
        await admin.Call("/admin/quota", new
        {
            id = aliceId,
            delta = 20
        });
        Assert.Equal(20, (await alice.Call("/me")).GetProperty("quota").GetInt32());
        var bobId = (await bob.Call("/me")).GetProperty("id").GetInt64();
        var ledger = await admin.Call("/admin/quota?id=" + bobId);
        Assert.Equal(1, ledger.GetProperty("items").EnumerateArray().Count(row => row.GetProperty("reason").GetString() == "refund"));
    }
    private static string Bodies(JsonElement conversation) => string.Join('|',
        conversation.GetProperty("messages").EnumerateArray().Select(row => row.GetProperty("body").GetString()));
}
