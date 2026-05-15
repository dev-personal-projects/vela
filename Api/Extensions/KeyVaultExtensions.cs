using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FinanceTracker.Api.Extensions;

public static class KeyVaultExtensions
{
    private const string _sectionName = "AzureKeyVault";
    private const string _managedIdentityMode = "ManagedIdentity";
    private const string _azureCliMode = "AzureCli";

    private static readonly IReadOnlyDictionary<string, string> _requiredSecrets = new Dictionary<string, string>
    {
        ["Auth--JwtSecret"] = "Auth:JwtSecret",
        ["ConnectionStrings--Default"] = "ConnectionStrings:Default",
        ["Supabase--Url"] = "Supabase:Url",
        ["Supabase--AnonKey"] = "Supabase:AnonKey",
        ["Redis--ConnectionString"] = "Redis:ConnectionString"
    };

    public static async Task<ConfigurationManager> AddFinanceTrackerKeyVaultAsync(
        this ConfigurationManager configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var vaultUriRaw = configuration[$"{_sectionName}:VaultUri"];
        if (string.IsNullOrWhiteSpace(vaultUriRaw))
        {
            throw new InvalidOperationException("AzureKeyVault:VaultUri is required.");
        }

        if (!Uri.TryCreate(vaultUriRaw, UriKind.Absolute, out var vaultUri))
        {
            throw new InvalidOperationException("AzureKeyVault:VaultUri must be an absolute URI.");
        }

        EnsureSecretsAreNotSuppliedByBootstrapConfiguration(configuration);

        var credential = CreateCredential(configuration, environment);
        var secretClient = new SecretClient(vaultUri, credential);
        var secrets = await LoadRequiredSecretsAsync(secretClient, cancellationToken);

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
        foreach (var configKey in _requiredSecrets.Values)
        {
            if (!string.IsNullOrWhiteSpace(configuration[configKey]))
            {
                throw new InvalidOperationException($"{configKey} must be supplied by Azure Key Vault, not bootstrap configuration.");
            }
        }
    }

    private static async Task<IReadOnlyDictionary<string, string?>> LoadRequiredSecretsAsync(
        SecretClient secretClient,
        CancellationToken cancellationToken)
    {
        var secretTasks = _requiredSecrets.Select(async item =>
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
