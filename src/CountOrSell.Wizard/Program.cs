using CountOrSell.Wizard.Models;
using CountOrSell.Wizard.Services;
using CountOrSell.Wizard.Steps;

Console.WriteLine("CountOrSell First-Run Wizard");
Console.WriteLine("============================");
Console.WriteLine();

var config = new WizardConfig();
var runner = new CommandRunner();

await Step01_DeploymentType.RunAsync(config);
await Step02_Prerequisites.RunAsync(config, new PrerequisiteChecker(runner));
await Step03_DockerRegistry.RunAsync(config);
await Step04_EnvironmentConfig.RunAsync(config, runner);
await Step05_HostingPreferences.RunAsync(config);
await Step06_SslCertificate.RunAsync(config);
await Step07_Branding.RunAsync(config);
await Step08_DatabaseAdminAccount.RunAsync(config);
await Step09_ProductAdminAccount.RunAsync(config);
await Step10_GeneralUserAccount.RunAsync(config);
await Step11_BackupDestination.RunAsync(config);
await Step12_BackupSchedule.RunAsync(config);
await Step13_BackupRetention.RunAsync(config);
await Step14_InitialUpdate.RunAsync(config);
await Step15_GenerateFiles.RunAsync(config);
await Step16_Deploy.RunAsync(config);
await Step17_UpdateCheckTime.RunAsync(config);

Console.WriteLine();
Console.WriteLine("Setup complete. CountOrSell is ready.");
