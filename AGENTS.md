# Guidelines for DotnetLegacyMigrator

- All non-example projects, tests, and CI must target **.NET 9.0**.
- Keep the repository's `global.json`, project files, and GitHub Actions in sync with the .NET 9 SDK.
- Always try to resolve build warnings before completing a task.
- Example projects may target different frameworks only when the legacy technology being demonstrated requires it.
- When asserting generated output in tests, store the expected code under `tests/Translation.Tests/Expected` (or a relevant subfolder) rather than embedding it inline within the test source.
- Ensure the .NET SDK version specified in `global.json` (currently `9.0.303`) is installed before running `dotnet` commands. If absent, install it using the official script:

  ```bash
  curl -L https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
  bash dotnet-install.sh -v 9.0.303
  export PATH="$HOME/.dotnet:$PATH"
  ```
