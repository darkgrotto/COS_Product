using CountOrSell.Wizard.Models;
using CountOrSell.Wizard.Services;
using CountOrSell.Wizard.Steps;

Console.WriteLine("CountOrSell First-Run Wizard");
Console.WriteLine("============================");
Console.WriteLine();

var config = new WizardConfig();
var runner = new CommandRunner();

await Step01_DeploymentType.RunAsync(config);
ConfigFileLoader.Load(config);
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
await Step14_GenerateFiles.RunAsync(config);
bool deployed = await Step15_Deploy.RunAsync(config);

if (deployed)
{
    await Step16_UpdateCheckTime.RunAsync(config);
    Console.WriteLine();
    if (Step15_Deploy.AccountsCreated)
    {
        Console.WriteLine("Setup complete. CountOrSell is ready.");
    }
    else
    {
        // The deployment succeeded, but the accounts the operator entered were not created
        // because the instance already had users. Saying "setup complete" here would send
        // them to a sign-in page that rejects the credentials they just chose.
        Console.WriteLine("Deployment complete, but setup did NOT finish: the accounts you");
        Console.WriteLine("entered were not created, because this instance already has users.");
        Console.WriteLine("See the warning above for how to proceed.");
    }
}
else
{
    Console.WriteLine();
    Console.WriteLine("Deployment did not complete successfully. Review the errors above.");
    Console.WriteLine("Your infrastructure may be partially provisioned.");
    Console.WriteLine("Use the undo script to revert any changes before retrying.");
}
