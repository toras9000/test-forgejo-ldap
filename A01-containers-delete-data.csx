#!/usr/bin/env dotnet-script
#r "nuget: Lestaly, 0.69.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

await Paved.RunAsync(config: c => c.AnyPause(), action: async () =>
{
    WriteLine("Stop service & delete volume ...");
    var composeFile = ThisSource.RelativeFile("./docker/compose.yml");
    await "docker".args("compose", "--file", composeFile.FullName, "down", "--remove-orphans", "--volumes").result().success();
});
