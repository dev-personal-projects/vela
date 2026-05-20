using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Vela.Api.Extensions;

public static class KeyVaultExtensions
{
    private const string _sectionName = "AzureKeyVault";
    private const string _managedIdentityMode = "ManagedIdentity";
    private const string _azureCliMode = "AzureCli";

    private static readonly IReadOnlyDictionary<string, string> _requiredKeyVaultSecrets = new Dictionary<string, string>
    {
        ["Auth--JwtSecret"] = "Auth:JwtSecret",
        ["ConnectionStrings--Default"] = "ConnectionStrings:Default",
        ["Supabase--Url"] = "Supabase:Url",
        ["Supabase--AnonKey"] = "Supabase:AnonKey",
        ["Redis--ConnectionString"] = "Redis:ConnectionString"
    };

    private static readonly string[] _requiredLocalDevelopmentSecrets =
    [
        "Auth:JwtSecret",
        "ConnectionStrings:Default",
        "Supabase:Url",
        "Supabase:AnonKey"
    ];

    public static async Task<ConfigurationManager> AddVelaKeyVaultAsync(
        this ConfigurationManager configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var vaultUriRaw = configuration[$"{_sectionName}:VaultUri"];
        if (string.IsNullOrWhiteSpace(vaultUriRaw))
        {
            if (environment.IsDevelopment())
            {
                EnsureLocalDevelopmentSecrets(configuration, "Azure Key Vault is not configured.");
                return configuration;
            }

            throw new InvalidOperationException("AzureKeyVault:VaultUri is required.");
        }

        if (!Uri.TryCreate(vaultUriRaw, UriKind.Absolute, out var vaultUri))
        {
            if (environment.IsDevelopment())
            {
                EnsureLocalDevelopmentSecrets(configuration, "AzureKeyVault:VaultUri is not an absolute URI.");
                return configuration;
            }

            throw new InvalidOperationException("AzureKeyVault:VaultUri must be an absolute URI.");
        }

        if (!environment.IsDevelopment())
        {
            EnsureSecretsAreNotSuppliedByBootstrapConfiguration(configuration);
        }

        IReadOnlyDictionary<string, string?> secrets;
        try
        {
            var credential = CreateCredential(configuration, environment);
            var secretClient = new SecretClient(vaultUri, credential);
            secrets = await LoadRequiredSecretsAsync(secretClient, cancellationToken);
        }
        catch (Exception ex) when (environment.IsDevelopment() && IsLocalDevelopmentFallbackAllowed(ex))
        {
            EnsureLocalDevelopmentSecrets(configuration, $"Azure Key Vault could not be loaded: {ex.Message}");
            return configuration;
        }

        configuration.AddInMemoryCollection(secrets);

        return configuration;
    }

    private static TokenCredential CreateCredential(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var mode = configuration[$"{_sectionName}:CredentialMode"];
        if (string.IsNullOrWhiteSpace(mode))
        {
            throw new InvalidOperationException("AzureKeyVault:CredentialMode is required.");
        }

        return mode.Trim() switch
        {
            _managedIdentityMode => CreateManagedIdentityCredential(configuration),
            _azureCliMode when environment.IsDevelopment() => new AzureCliCredential(),
            _azureCliMode => throw new InvalidOperationException("AzureCli Key Vault authentication is allowed only in Development."),
            _ => throw new InvalidOperationException("AzureKeyVault:CredentialMode must be ManagedIdentity or AzureCli.")
        };
    }

    private static ManagedIdentityCredential CreateManagedIdentityCredential(IConfiguration configuration)
    {
        var clientId = configuration[$"{_sectionName}:ManagedIdentityClientId"];

        return string.IsNullOrWhiteSpace(clientId)
            ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
            : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(clientId.Trim()));
    }

    private static void EnsureSecretsAreNotSuppliedByBootstrapConfiguration(IConfiguration configuration)
    {
        foreach (var configKey in _requiredKeyVaultSecrets.Values)
        {
            if (!string.IsNullOrWhiteSpace(configuration[configKey]))
            {
                throw new InvalidOperationException($"{configKey} must be supplied by Azure Key Vault, not bootstrap configuration.");
            }
        }
    }

    private static void EnsureLocalDevelopmentSecrets(IConfiguration configuration, string fallbackReason)
    {
        var missingSecrets = _requiredLocalDevelopmentSecrets
            .Where(configKey => string.IsNullOrWhiteSpace(configuration[configKey]))
            .ToArray();

        if (missingSecrets.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{fallbackReason} Configure local development secrets in local.settings.json Values: {string.Join(", ", missingSecrets.Select(ToEnvironmentVariableName))}.");
    }

    private static bool IsLocalDevelopmentFallbackAllowed(Exception exception)
    {
        return exception is InvalidOperationException or RequestFailedException;
    }

    private static string ToEnvironmentVariableName(string configKey)
    {
        return configKey.Replace(":", "__", StringComparison.Ordinal);
    }

    private static async Task<IReadOnlyDictionary<string, string?>> LoadRequiredSecretsAsync(
        SecretClient secretClient,
        CancellationToken cancellationToken)
    {
        var secretTasks = _requiredKeyVaultSecrets.Select(async item =>
        {
            KeyVaultSecret secret;
            try
            {
                secret = await secretClient.GetSecretAsync(item.Key, cancellationToken: cancellationToken);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                throw new InvalidOperationException($"Required Key Vault secret '{item.Key}' is missing.", ex);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException($"Could not read required Key Vault secret '{item.Key}'.", ex);
            }

            ValidateSecret(item.Key, secret);

            return KeyValuePair.Create(item.Value, (string?)secret.Value);
        });

        return (await Task.WhenAll(secretTasks)).ToDictionary(x => x.Key, x => x.Value);
    }

    private static void ValidateSecret(string secretName, KeyVaultSecret secret)
    {
        if (secret.Properties.Enabled == false)
        {
            throw new InvalidOperationException($"Required Key Vault secret '{secretName}' is disabled.");
        }

        if (string.IsNullOrWhiteSpace(secret.Value))
        {
            throw new InvalidOperationException($"Required Key Vault secret '{secretName}' is empty.");
        }
    }

}
