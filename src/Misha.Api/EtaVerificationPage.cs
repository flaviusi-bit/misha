using System.Security.Cryptography;

namespace Misha.Api;

public static class EtaVerificationPage
{
    public static string Create(string nonce)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        return $"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>eTA verification</title>
  <style nonce="{nonce}">
    :root {{ color-scheme: light dark; font-family: system-ui, sans-serif; }}
    body {{ margin: 0; min-height: 100vh; display: grid; place-items: center; background: #f4f6f8; }}
    main {{ width: min(92vw, 32rem); padding: 2rem; border-radius: 1rem; background: Canvas; box-shadow: 0 8px 30px rgb(0 0 0 / 12%); text-align: center; }}
    h1 {{ margin-top: 0; }}
    #status {{ margin: 1.5rem 0 0; font-size: 1.1rem; }}
    .valid {{ color: #18794e; }}
    .invalid {{ color: #b42318; }}
  </style>
</head>
<body>
  <main>
    <h1>eTA verification</h1>
    <p id="status">Verifying…</p>
  </main>
  <script nonce="{nonce}">
    (() => {{
      const status = document.getElementById('status');
      const etaNumber = decodeURIComponent(location.pathname.split('/').pop() || '');
      const token = new URLSearchParams(location.hash.slice(1)).get('token');

      if (!etaNumber || !token) {{
        status.textContent = 'This verification link is incomplete.';
        status.className = 'invalid';
        return;
      }}

      fetch('/eta/verify', {{
        method: 'POST',
        headers: {{ 'Content-Type': 'application/json' }},
        body: JSON.stringify({{ etaNumber, verificationToken: token }})
      }})
        .then(async response => {{
          if (!response.ok) throw new Error('verification-failed');
          return response.json();
        }})
        .then(result => {{
          const expiry = new Date(result.expiresAtUtc).toLocaleDateString();
          status.textContent = `Status: ${{result.status}} · Expires: ${{expiry}}`;
          status.className = result.status === 'Valid' ? 'valid' : 'invalid';
          history.replaceState(null, document.title, location.pathname);
        }})
        .catch(() => {{
          status.textContent = 'The eTA could not be verified.';
          status.className = 'invalid';
        }});
    }})();
  </script>
</body>
</html>""";
    }

    public static string CreateNonce()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }

    public static void ApplySecurityHeaders(HttpResponse response, string nonce)
    {
        response.Headers["Content-Security-Policy"] =
            $"default-src 'none'; script-src 'nonce-{nonce}'; style-src 'nonce-{nonce}'; connect-src 'self'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Cache-Control"] = "no-store";
    }
}
