#!/usr/bin/env dotnet-script
#r "nuget: Lestaly, 0.79.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(async () =>
{
    WriteLine("Stop service & delete volume ...");
    var composeFile = ThisSource.RelativeFile("./docker/compose.yml");
    await "docker".args("compose", "--file", composeFile, "down", "--remove-orphans", "--volumes").result().success();
});
