#!/usr/bin/env dotnet-script
#r "nuget: Lestaly, 0.79.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(async () =>
{
    WriteLine($"Restart service ...");
    var composeFile = ThisSource.RelativeFile("./docker/compose.yml");
    await "docker".args("compose", "--file", composeFile, "down", "--remove-orphans").echo().result().success();
    await "docker".args("compose", "--file", composeFile, "up", "-d", "--wait").echo().result().success();
    WriteLine("Container up completed.");
});
