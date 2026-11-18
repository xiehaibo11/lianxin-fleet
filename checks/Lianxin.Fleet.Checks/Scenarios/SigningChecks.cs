using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Cli.Commands;
using Lianxin.Fleet.Cli.Signing;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static class SigningChecks
{
    public static Task Profiles()
    {
        const string xml = """
            <plist version="1.0"><dict>
              <key>UUID</key><string>11111111-1111-1111-1111-111111111111</string>
              <key>TeamIdentifier</key><array><string>TESTTEAM01</string></array>
              <key>ApplicationIdentifierPrefix</key><array><string>TESTTEAM01</string></array>
              <key>ExpirationDate</key><date>2027-11-17T00:00:00Z</date>
              <key>Entitlements</key><dict><key>application-identifier</key><string>TESTTEAM01.com.example.*</string><key>get-task-allow</key><true/></dict>
              <key>ProvisionedDevices</key><array><string>00008030-0012345678900001</string></array>
              <key>DeveloperCertificates</key><array><data>AQID</data></array>
            </dict></plist>
            """;
        var profile = IosProfile.Parse(xml);
        var now = new DateTimeOffset(2026, 11, 17, 0, 0, 0, TimeSpan.Zero);
        profile.Validate("TESTTEAM01", "com.example.internal", "debugging", ["00008030-0012345678900001"], now);
        Assert.Equal(1, profile.Certificates.Length);
        Assert.Throws<ArgumentException>(() => profile.Validate("OTHERTEAM1", "com.example.internal", "debugging", [], now));
        Assert.Throws<ArgumentException>(() => profile.Validate("TESTTEAM01", "com.unrelated.app", "debugging", [], now));
        Assert.Throws<ArgumentException>(() => profile.Validate("TESTTEAM01", "com.example.internal", "release-testing", [], now));
        Assert.Throws<ArgumentException>(() => profile.Validate("TESTTEAM01", "com.example.internal", "enterprise", [], now));
        Assert.Throws<ArgumentException>(() => profile.Validate("TESTTEAM01", "com.example.internal", "debugging", ["unregistered"], now));
        Assert.Throws<ArgumentException>(() => profile.Validate("TESTTEAM01", "com.example.internal", "debugging", [], now.AddYears(2)));
        Assert.Throws<ArgumentException>(() => Options.Parse(["--team", "one", "--team", "two"]));
        return Task.CompletedTask;
    }
}
