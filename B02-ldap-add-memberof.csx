#!/usr/bin/env dotnet-script
#r "nuget: Lestaly, 0.79.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(async () =>
{
    var composeFile = ThisSource.RelativeFile("./docker/compose.yml");
    await "docker".args(
        "compose", "--file", composeFile.FullName, "exec", "ldap",
        "ldapadd", "-Q", "-Y", "EXTERNAL", "-H", "ldapi:///", "-f", "/ldifs/memberof.ldif"
    );
});
