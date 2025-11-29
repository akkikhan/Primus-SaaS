# Publishing Actions (npm / NuGet / GitHub Releases)

This repo is set to `approval_policy: never`, so the actual publish steps must be run by a maintainer. Use this checklist to finish the release tasks noted in PROJECT_STATUS.

## npm (Node packages)
- [ ] `cd sdk/nodejs/primus-identity-validator` and bump version in `package.json`.
- [ ] `npm login` (if not already).
- [ ] `npm publish --access public`.
- [ ] Verify: `npm view @primus-saas/identity-validator version`.

## NuGet (.NET packages)
- [ ] `cd sdk/logging/dotnet/PrimusSaaS.Logging` and bump `<Version>` in `.csproj`.
- [ ] `dotnet pack -c Release`.
- [ ] `dotnet nuget push bin/Release/*.nupkg -k <API_KEY> -s https://api.nuget.org/v3/index.json`.
- [ ] Repeat for `sdk/dotnet/Primus.Notifications` and `sdk/dotnet/PrimusSaaS.Identity.Validator`.
- [ ] Verify: `dotnet nuget list source` and `nuget list PrimusSaaS.* -Source https://api.nuget.org/v3/index.json`.

## GitHub Releases
- [ ] Tag versions per package (e.g., `git tag logging-vX.Y.Z`, `notifications-vX.Y.Z`, `identity-vX.Y.Z`).
- [ ] `git push origin --tags`.
- [ ] Create GitHub releases with changelog notes and attach `.nupkg`/`.tgz` artifacts as needed.
