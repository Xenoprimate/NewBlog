using System.Security.Cryptography;
using System.Text;

namespace BenBowenBlog.Web.Admin;

public enum AdminKeyResult
{
    Valid,
    Invalid,
    Busy
}

// Checks the admin key (configured as Admin:Key). Attempts are processed one at a time and every failure holds
// the gate for FailureDelay, which caps guessing at roughly one attempt per second across the whole site.
public sealed class AdminKey(IConfiguration configuration)
{
    private static readonly TimeSpan FailureDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<AdminKeyResult> VerifyAsync(string? candidate, CancellationToken cancellationToken)
    {
        if (!await gate.WaitAsync(GateTimeout, cancellationToken))
        {
            return AdminKeyResult.Busy;
        }

        try
        {
            if (Matches(candidate))
            {
                return AdminKeyResult.Valid;
            }

            // Deliberately not cancellable: aborting the request must not release the gate early.
            await Task.Delay(FailureDelay, CancellationToken.None);
            return AdminKeyResult.Invalid;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool Matches(string? candidate)
    {
        var configured = configuration["Admin:Key"];

        // Fail closed: with no key configured, nothing unlocks.
        if (string.IsNullOrEmpty(configured) || string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        // Hashing first makes both inputs the same length, so the comparison takes constant time.
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(candidate)),
            SHA256.HashData(Encoding.UTF8.GetBytes(configured)));
    }
}
