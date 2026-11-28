namespace PeopleHub.Domain.Entities;

/// <summary>US-PH3-001 registry entry for an external integration. Holds metadata only. The credential itself never enters this system: CredentialRef is a label that names the item in the approved secrets store.</summary>
public class IntegrationEntry
{
 public int Id { get; set; }
 public string Name { get; set; } = "";
 public string Purpose { get; set; } = "";
 public string OwnerName { get; set; } = "";
 public string SupportedVersion { get; set; } = "";
 /// <summary>None, ApiKey, OAuth2, Basic or Certificate.</summary>
 public string AuthMethod { get; set; } = "None";
 /// <summary>Disabled (default) or Enabled. Enabling only marks intent: no adapter exists yet.</summary>
 public string Status { get; set; } = "Disabled";
 public string CredentialRef { get; set; } = "";
 public DateTime? CredentialRotatedAt { get; set; }
 public string Actor { get; set; } = "";
 public DateTime At { get; set; }
}
