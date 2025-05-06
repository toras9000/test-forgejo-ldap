#r "nuget: AngleSharp, 1.3.0"
#r "nuget: Lestaly, 0.79.0"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Kokuban;
using Lestaly;
using Lestaly.Cx;

var settings = new
{
    ComposeFile = ThisSource.RelativeFile("./docker/compose.yml"),

    Auth = new
    {
        Name = "myserver-ldap",
        Host = "host.docker.internal",
        Port = 389,
        Security = SecurityProtocol.Unencrypted,
        SearchBaseDn = "ou=persons,ou=accounts,dc=myserver,o=home",
        UserFilter = "(&(objectClass=inetOrgPerson)(|(uid=%[1]s)(mail=%[1]s)))",
        AttrUser = "uid",
        AttrGivenname = "givenName",
        AttrSurname = "sn",
        AttrMailaddr = "mail",
        Group = new
        {
            Enabled = false,
            SearchBaseDn = "ou=groups,dc=myserver,o=home",
            AttrMember = "member",
            AttrMemberFormat = "dn",
            GroupFilter = "",
            GroupTeamMap = """
            {
                "cn=developers,ou=groups,dc=myserver,o=home":
                {
                    "MyForgejoOrganization": ["developers"]
                }
            }
            """,
        },
    },
};

enum SecurityProtocol
{
    Unencrypted,
    LDAPS,
    StartTLS,
}

return await Paved.ProceedAsync(async () =>
{
    using var signal = new SignalCancellationPeriod();
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    await "docker".args(
        "compose", "--file", settings.ComposeFile, "exec", "-u", "1000", "app",
        "forgejo", "admin", "auth", "add-ldap",
            "--active",
            "--name", settings.Auth.Name,
            "--host", settings.Auth.Host,
            "--port", $"{settings.Auth.Port}",
            "--security-protocol", $"{settings.Auth.Security}",
            "--user-search-base", settings.Auth.SearchBaseDn,
            "--user-filter", settings.Auth.UserFilter,
            "--username-attribute", settings.Auth.AttrUser,
            "--firstname-attribute", settings.Auth.AttrGivenname,
            "--surname-attribute", settings.Auth.AttrSurname,
            "--email-attribute", settings.Auth.AttrMailaddr,
            "--synchronize-users"
    ).echo().cancelby(signal.Token).result().success().output();

    if (settings.Auth.Group.Enabled)
    {
        WriteLine(Chalk.Yellow["Currently, there is no automated I/F to perform group synchronization."]);
    }

    WriteLine("LDAP auth addtion completed.");
});
