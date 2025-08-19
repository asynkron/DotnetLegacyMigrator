# DotnetLegacyMigrator

## Project Goals

DotnetLegacyMigrator helps teams modernize older .NET applications by
translating legacy data access patterns into Entity Framework Core code
that runs on .NET 9.

## Supported Legacy Technologies

- Typed DataSets
- LINQ to SQL
- NHibernate `.hbm.xml` mappings
- Additional legacy frameworks planned for future support

## Quickstart

1. Install the [`.NET 9.0.303 SDK`](https://dotnet.microsoft.com/download). If it's not installed, you can add it locally with:

   ```bash
   curl -L https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
   bash dotnet-install.sh -v 9.0.303
   export PATH="$HOME/.dotnet:$PATH"
   ```
2. Clone the repository:

   ```bash
   git clone https://github.com/your-org/DotnetLegacyMigrator.git
   cd DotnetLegacyMigrator
   ```

3. Run the CLI against an example project:

   ```bash
   dotnet run --project src/Cli
   ```

## Building

Restore and build all projects in the solution:

```bash
dotnet build
```

## Running the CLI

Invoke the command-line interface to select an example and inspect the
generated output:

```bash
dotnet run --project src/Cli
```

## Contributing

Contributions are welcome!

1. Fork the repository and create a new branch.
2. Run the tests and lint the Markdown before submitting:

   ```bash
   dotnet test
   npx markdownlint README.md
   ```

3. Open a pull request describing your changes.
