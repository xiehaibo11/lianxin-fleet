using System.Security.Cryptography;
using System.Text;
using Lianxin.Sms.Checks.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Lianxin.Sms.Checks.Scenarios;

internal static class CompatibilityChecks
{
    public static async Task Run()
    {
        await using var fixture = await HttpFixture.Create(false);
        using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = fixture.DatabasePath }.ToString());
        await db.OpenAsync();
        // Fixture uses the previous Node scrypt defaults: N=16384, r=8, p=1, 64-byte key, UTF-8 salt.
        const string record = "legacy-salt-test-only-2026:a25ba7dec3ebccba35fe024d20a6f13696b21c017f9408a5f85ba367314e9559c27e5eac1e4d5667c849f93828045eccde35f68418862f117485698a394da59a";
        using (var command = db.CreateCommand())
        {
            command.CommandText = "UPDATE users SET password=$record WHERE email='admin@lianxin.local'";
            command.Parameters.AddWithValue("$record", record);
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }
        var admin = await fixture.Login("admin@lianxin.local");
        var adminId = (await admin.Call("/me")).GetProperty("id").GetInt64();
        var device = await admin.Call("/admin/devices", new
        {
            name = "Legacy iPhone",
            line = "+12025550201"
        }, 201);
        var deviceToken = new string('A', 43);
        var sessionToken = new string('B', 43);
        using (var command = db.CreateCommand())
        {
            command.CommandText = "UPDATE devices SET token=$hash WHERE id=$id";
            command.Parameters.AddWithValue("$hash", Hash(deviceToken));
            command.Parameters.AddWithValue("$id", device.GetProperty("id").GetInt64());
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }
        using (var command = db.CreateCommand())
        {
            command.CommandText = "INSERT INTO sessions(token,user_id,expires_at) VALUES($hash,$id,$expires)";
            command.Parameters.AddWithValue("$hash", Hash(sessionToken));
            command.Parameters.AddWithValue("$id", adminId);
            command.Parameters.AddWithValue("$expires", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds());
            await command.ExecuteNonQueryAsync();
        }
        var poll = await fixture.Client(deviceToken).Call("/device/poll", new
        {
            canSend = true
        });
        Assert.Equal("Legacy iPhone", poll.GetProperty("device").GetProperty("name").GetString());
        var me = await fixture.Client(cookie: "lianxin=" + sessionToken).Call("/me");
        Assert.Equal(adminId, me.GetProperty("id").GetInt64());
        await admin.Call("/admin/accounts", new
        {
            email = "legacy@example.test",
            name = "Legacy",
            tenant = "旧租户",
            password = HttpFixture.Password,
            quota = 5
        }, 201);
        var member = await fixture.Login("legacy@example.test");
        var memberId = (await member.Call("/me")).GetProperty("id").GetInt64();
        using (var command = db.CreateCommand())
        {
            command.CommandText = "INSERT INTO send_requests(user_id,request_key,fingerprint,result) VALUES($id,'legacy-send',$hash,$result)";
            command.Parameters.AddWithValue("$id", memberId);
            command.Parameters.AddWithValue("$hash", "1996a8ae7d37458929e12a866dd6fc8d21572e36470b4bf181b6b31e7f627376");
            command.Parameters.AddWithValue("$result", "{\"conversation\":123,\"count\":1}");
            await command.ExecuteNonQueryAsync();
        }
        var previous = await member.Call("/messages", new
        {
            key = "legacy-send",
            recipients = new[] { "+12025550191" },
            body = "兼容\n测试🙂"
        }, 201);
        Assert.Equal(123L, previous.GetProperty("conversation").GetInt64());
        Assert.Equal(5, (await member.Call("/me")).GetProperty("quota").GetInt32());
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
