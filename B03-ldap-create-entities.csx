#!/usr/bin/env dotnet-script
#r "nuget: Lestaly, 0.69.0"
#r "nuget: Kokuban, 0.2.0"
#load ".directory-service-extensions.csx"
#nullable enable
using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Kokuban;
using Lestaly;

var settings = new
{
    // LDAP server settings
    Server = new
    {
        // Host name or ip
        Host = "myserver.home",

        // Port number
        Port = 389,

        // Use SSL
        Ssl = false,

        // LDAP protocol version
        ProtocolVersion = 3,
    },

    Directory = new
    {
        // Bind user credential, null is anonymous
        BindCredential = new NetworkCredential("uid=configurator,ou=operators,dc=myserver,o=home", "configurator-pass"),

        // Group manage unit DN
        GroupUnitDn = "ou=groups,dc=myserver,o=home",

        // User manage unit Base
        UserUnitDn = "ou=accounts,dc=myserver,o=home",
    },

    Definitions = new DefineGroup[]
    {
        new(Group: "developers", Members:
        [
            new(Uid:"user1", Password: "user1", Unit:"persons"),
            new(Uid:"user2", Password: "user2", Unit:"persons"),
            new(Uid:"user3", Password: "user3", Unit:"persons"),
        ]),
        new(Group: "guests", Members:
        [
            new(Uid:"user3", Password: "user3", Unit:"persons"),
        ]),
        new(Group: "alpha", Members:
        [
            new(Uid:"userA", Password: "userA", Unit:"persons"),
            new(Uid:"userB", Password: "userB", Unit:"persons"),
            new(Uid:"userC", Password: "userC", Unit:"persons"),
        ]),
        new(Group: "bravo", Members:
        [
            new(Uid:"userC", Password: "userC", Unit:"persons"),
            new(Uid:"userD", Password: "userD", Unit:"persons"),
            new(Uid:"userE", Password: "userE", Unit:"persons"),
        ]),
        new(Group: "charlie", Members:
        [
            new(Uid:"userA", Password: "userA", Unit:"persons"),
            new(Uid:"userD", Password: "userD", Unit:"persons"),
        ]),
    },

};

record DefineUser(string Uid, string? Cn = default, string? Sn = default, string? Mail = default, string? Password = default, string? Unit = default)
{
    public string FullDn(string baseDn) => this.Unit == null ? $"uid={this.Uid},{baseDn}" : $"uid={this.Uid},ou={this.Unit},{baseDn}";
};
record DefineGroup(string Group, DefineUser[] Members);

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    // Bind to LDAP server
    WriteLine("Bind to LDAP server");
    var server = new LdapDirectoryIdentifier(settings.Server.Host, settings.Server.Port);
    using var ldap = new LdapConnection(server);
    ldap.SessionOptions.SecureSocketLayer = settings.Server.Ssl;
    ldap.SessionOptions.ProtocolVersion = settings.Server.ProtocolVersion;
    ldap.AuthType = AuthType.Basic;
    ldap.Credential = settings.Directory.BindCredential;
    ldap.Bind();
    WriteLine(Chalk.Green[$".. OK"]);

    WriteLine("Generate users");
    foreach (var user in settings.Definitions.SelectMany(g => g.Members))
    {
        WriteLine($".. User={user.Uid}");
        var userDn = user.FullDn(settings.Directory.UserUnitDn);
        var userEntry = await ldap.GetEntryOrDefaultAsync(userDn);
        if (userEntry != null)
        {
            WriteLine($".... Alredy exists");
            continue;
        }
        try
        {
            await ldap.CreateEntryAsync(userDn,
            [
                new("objectClass", ["inetOrgPerson", "extensibleObject"]),
                new("cn", user.Cn ?? user.Uid),
                new("sn", user.Sn ?? user.Uid),
                new("mail", user.Mail ?? $"{user.Uid}@example.com"),
                new("userPassword", MakeHashPassword(user.Password ?? user.Uid)),
            ]);
            WriteLine(Chalk.Green[$".... Created: {userDn}"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$".... Error: {ex.Message}"]);
        }
    }

    WriteLine("Generate groups");
    foreach (var define in settings.Definitions)
    {
        WriteLine($".. Group={define.Group}");
        var groupDn = $"cn={define.Group},{settings.Directory.GroupUnitDn}";
        var groupEntry = await ldap.GetEntryOrDefaultAsync(groupDn);
        if (groupEntry != null)
        {
            WriteLine($".... Alredy exists");
            continue;
        }
        try
        {
            var members = define.Members.Select(m => m.FullDn(settings.Directory.UserUnitDn)).ToArray();
            await ldap.CreateEntryAsync(groupDn,
            [
                new("objectClass", "groupOfNames"),
                    new("cn", define.Group),
                    new("member", members),
                ]);
            WriteLine(Chalk.Green[$".... Created: {groupDn}"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$".... Error: {ex.Message}"]);
        }
    }
});

string MakeHashPassword(string input)
{
    var salt = new byte[4];
    Random.Shared.NextBytes(salt);
    var source = Encoding.UTF8.GetBytes(input);
    var hashed = SHA256.HashData(source.Concat(salt).ToArray());
    var encoded = Convert.ToBase64String(hashed.Concat(salt).ToArray());
    var value = $"{{SSHA256}}{encoded}";

    return value;
}