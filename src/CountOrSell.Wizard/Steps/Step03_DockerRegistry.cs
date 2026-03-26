using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Steps;

public static class Step03_DockerRegistry
{
    private const string DefaultRegistry = "ghcr.io/darkgrotto/countorsell";

    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 3 of 17: Docker Image");
        Console.WriteLine("--------------------------");

        config.ConfigValues.TryGetValue("docker_registry", out var cfgRegistry);
        config.ConfigValues.TryGetValue("docker_image_tag", out var cfgTag);

        if (config.DeploymentType == DeploymentType.Docker)
        {
            Console.WriteLine("Specify the Docker image registry and organization where CountOrSell images are hosted.");
            Console.WriteLine($"Default ({DefaultRegistry}) pulls from the official CountOrSell registry.");
            Console.WriteLine("Use a custom value only if you are hosting the image in your own registry.");
            Console.WriteLine();

            if (config.AutoAccept && cfgRegistry != null)
            {
                config.DockerRegistry = cfgRegistry;
                Console.WriteLine($"Docker image registry: {cfgRegistry}");
            }
            else
            {
                var defaultRegistry = cfgRegistry ?? DefaultRegistry;
                Console.Write($"Docker image registry [{defaultRegistry}]: ");
                var registryInput = Console.ReadLine()?.Trim();
                config.DockerRegistry = string.IsNullOrEmpty(registryInput) ? defaultRegistry : registryInput;
                Console.WriteLine($"Registry set to: {config.DockerRegistry}");
            }
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine($"CountOrSell images are pulled from {DefaultRegistry}.");
            Console.WriteLine();
        }
        string tag;
        if (config.AutoAccept && cfgTag != null)
        {
            tag = cfgTag;
            Console.WriteLine($"Image tag: {tag}");
            if (tag != "latest")
            {
                Console.WriteLine($"WARNING: Tag \"{tag}\" is configured. Only latest is recommended for production use.");
            }
        }
        else
        {
            var defaultTag = cfgTag ?? "latest";
            Console.Write($"Image tag [{defaultTag}]: ");
            var tagInput = Console.ReadLine()?.Trim();
            tag = string.IsNullOrEmpty(tagInput) ? defaultTag : tagInput;

            if (tag != "latest")
            {
                Console.WriteLine();
                Console.WriteLine($"WARNING: You selected tag \"{tag}\".");
                Console.WriteLine("Only the latest tag is recommended for production use.");
                Console.WriteLine("Specific version tags may be used for testing or pinned deployments,");
                Console.WriteLine("but will not receive automatic updates.");
                Console.WriteLine();

                while (true)
                {
                    Console.Write($"Confirm use of tag \"{tag}\"? [y/N]: ");
                    var confirm = Console.ReadLine()?.Trim().ToUpperInvariant();
                    if (confirm == "Y" || confirm == "YES")
                    {
                        break;
                    }
                    if (string.IsNullOrEmpty(confirm) || confirm == "N" || confirm == "NO")
                    {
                        tag = "latest";
                        Console.WriteLine("Reverted to tag: latest");
                        break;
                    }
                }
            }
        }

        config.DockerImageTag = tag;
        Console.WriteLine($"Image tag set to: {config.DockerImageTag}");
        Console.WriteLine();
        return Task.CompletedTask;
    }
}
