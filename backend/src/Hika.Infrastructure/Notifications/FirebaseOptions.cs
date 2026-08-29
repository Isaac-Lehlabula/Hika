namespace Hika.Infrastructure.Notifications;

/// <summary>
/// Empty by default — the valid "no Firebase project configured yet" state, same pattern as
/// OzowOptions. See DependencyInjection.AddInfrastructure for the conditional registration this
/// gates, and FirebasePushSender's remarks for what's needed to actually go live.
/// </summary>
public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    /// <summary>The full JSON content of a Firebase service account key file (Project Settings →
    /// Service Accounts → Generate new private key, in the Firebase console) — not a path, so
    /// this can be set directly as a deployment secret/env var without shipping a file.</summary>
    public string ServiceAccountJson { get; set; } = "";
}
