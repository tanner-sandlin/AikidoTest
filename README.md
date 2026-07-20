# AikidoTest — Intentionally Vulnerable Sample App

A small ASP.NET MVC 5 (.NET Framework 4.7.2) web application, built to be
deployed under IIS, purpose-built to **evaluate a SAST/SCA tool**. It is
riddled with planted vulnerabilities on purpose. Do not deploy it anywhere
reachable from the internet, and don't reuse any code from it.

## Layout

```
AikidoTest.sln
AikidoTest.Web/
  Web.config              # security misconfig + hardcoded secrets
  packages.config          # outdated/vulnerable NuGet package pins
  Controllers/
    AccountController.cs   # SQLi, credential logging, weak hashing, insecure cookie
    CheckoutController.cs  # PCI-DSS log/storage violations, weak crypto, SQLi
    SearchController.cs    # reflected XSS, SQLi
    FilesController.cs     # path traversal
    AdminController.cs     # XXE, insecure deserialization, command injection
    RedirectController.cs  # open redirect
  Utils/
    AppLogger.cs            # unredacted file logger (the PCI logging sink)
    CryptoHelper.cs          # MD5 password hashing, DES "encryption"
  App_Data/schema.sql       # sample DB schema (plaintext PAN/CVV columns)
```

## Planted vulnerabilities

| # | Category | Location | CWE / Notes |
|---|----------|----------|-------------|
| 1 | SQL Injection | `AccountController.Login` | CWE-89, string-concatenated login query |
| 2 | SQL Injection | `CheckoutController.Index(POST)` | CWE-89, string-concatenated INSERT |
| 3 | SQL Injection | `SearchController.RunSearch` | CWE-89, string-concatenated LIKE query |
| 4 | Reflected XSS | `SearchController.Index` + `Views/Search/Index.cshtml` | CWE-79, `Html.Raw` on unencoded query string |
| 5 | Credential logging | `AccountController.Login` | CWE-532, plaintext username/password written to `App_Data/logs/app.log` |
| 6 | **PCI: cardholder data logging** | `CheckoutController.Index(POST)` | CWE-532 / PCI-DSS 3.2–3.4, full PAN **and CVV** written to log file |
| 7 | **PCI: CVV storage** | `CheckoutController.Index(POST)`, `App_Data/schema.sql` | PCI-DSS Req. 3.2 — CVV persisted to the `Orders` table, which is never allowed |
| 8 | **PCI: plaintext PAN storage** | `CheckoutController.Index(POST)`, `App_Data/schema.sql` | PCI-DSS Req. 3.4 — `CardNumber` column stored unencrypted alongside the weakly-encrypted copy |
| 9 | Weak cryptography (hashing) | `CryptoHelper.HashPassword` | CWE-327, unsalted MD5 for password storage |
| 10 | Weak cryptography (encryption) | `CryptoHelper.EncryptCardNumber` | CWE-327/CWE-326, DES with a hardcoded key/IV |
| 11 | Hardcoded secrets | `Web.config`, `AccountController`, `CheckoutController`, `CryptoHelper` | CWE-798, DB password, Stripe key, AWS key, encryption key all committed in source |
| 12 | XXE | `AdminController.ImportConfig` | CWE-611, `XmlDocument.LoadXml` with default (unsafe) resolver |
| 13 | Insecure deserialization | `AdminController.RestoreSession` | CWE-502, `BinaryFormatter.Deserialize` on an attacker-supplied cookie |
| 14 | OS command injection | `AdminController.Ping` | CWE-78, unsanitized input passed to `cmd.exe /c ping` |
| 15 | Path traversal | `FilesController.Download` | CWE-22, unsanitized filename joined into a file path |
| 16 | Open redirect | `RedirectController.Go` | CWE-601, unsanitized `url` parameter passed to `Redirect()` |
| 17 | Missing CSRF protection | `AccountController.Login(POST)` | CWE-352, no `[ValidateAntiForgeryToken]` |
| 18 | Insecure cookies | `AccountController.Login`, `Web.config` | CWE-614/1004, auth cookies without `Secure`/`HttpOnly`, `httpOnlyCookies="false"` |
| 19 | Debug mode / verbose errors | `Web.config`, `Global.asax.cs` | CWE-489/209, `debug="true"`, `customErrors mode="Off"`, exception text written to the response |
| 20 | Request validation disabled | `Web.config` | CWE-16, `validateRequest="false"` removes built-in reflected-XSS protection |
| 21 | Weak/hardcoded machineKey | `Web.config` | CWE-321, static `validationKey`/`decryptionKey` with `decryption="DES"` |
| 22 | Permissive CORS | `Web.config` | CWE-942, `Access-Control-Allow-Origin: *` |
| 23 | Directory browsing enabled | `Web.config` | CWE-548, `directoryBrowse enabled="true"` |
| 24 | Outdated/vulnerable dependencies | `packages.config` | SCA target: jQuery 1.4.1, Microsoft.AspNet.Mvc 5.0.0, Newtonsoft.Json 6.0.4, bootstrap 3.0.0, log4net 1.2.10, etc. |

## Notes for evaluating the SAST tool

- Everything above is annotated in-code with a `// CWE-xxx:` comment at the
  planted line, so you can quickly confirm true positives vs. what the tool
  actually flags.
- Table rows 6–8 are the PCI-specific findings — useful for checking whether
  the tool has PCI-DSS-aware rules (cardholder data logging/storage) versus
  only generic "sensitive data in logs" heuristics.
- `packages.config` pins deliberately ancient package versions for SCA
  (software composition analysis) coverage, separate from the SAST findings.
- The project references NuGet packages by hint path but the `packages/`
  folder is not included — restore isn't required for source-based SAST
  scanning, only for an actual `msbuild`/IIS deployment.
