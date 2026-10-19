# SecurePrint

**An authentication-based secure printing system for Windows.**

SecurePrint is a Windows desktop application designed to help prevent unauthorized printing by requiring user authentication before an intercepted print job is allowed to proceed.

The system uses a C# Windows background service, a desktop user interface, and named-pipe communication to coordinate print-job authorization.

## Features

- **Print-job interception:** Monitors print jobs through the Windows printing infrastructure.
- **User authentication:** Requests authentication before an intercepted job is authorized.
- **Background service:** Runs the backend separately from the desktop UI.
- **Desktop authentication interface:** Provides a UI for handling authentication requests.
- **Named-pipe communication:** Exchanges job information and authentication responses between the backend and UI.
- **Shared project:** Keeps common message types and data structures in a shared C# project.

> Note: These features describe the intended project design. Actual interception behavior can vary by printer, driver, and Windows configuration.

## Architecture

```text
SecurePrint
├── backend_service/
│   └── SecurePrint.BackendService/
├── frontend_ui/
│   └── SecurePrint.UserAgent/
├── shared/
│   └── SecurePrint.Shared/
├── data/
├── SecurePrint.slnx
└── .gitignore
```

### Components

| Component | Responsibility |
|---|---|
| Backend Service | Monitors and manages print-job authorization |
| UserAgent | Displays the authentication interface and communicates with the backend |
| Shared | Contains common data structures and message definitions |
| Data | Local project data; contents may vary by environment |
| SecurePrint.slnx | Solution file for opening the project |

## Prerequisites

- Windows operating system.
- Git.
- Visual Studio or VS Code with suitable C#/.NET development support.
- The .NET SDK and targeting components required by the project files.
- A compatible printer or virtual printer for testing.

The exact .NET version and Windows service permissions should be verified against the project files and your local environment.

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Rituraj-Singh17/SecurPrint.git
cd SecurPrint
```

Because the repository is private, collaborators must accept an invitation and authenticate with GitHub before cloning.

### 2. Open the solution

Open `SecurePrint.slnx` in a compatible version of Visual Studio, or open the project folder in VS Code.

### 3. Restore dependencies and build

Use the .NET tooling supported by your installed SDK and the project files. From the solution directory, you can try:

```bash
dotnet restore SecurePrint.slnx
dotnet build SecurePrint.slnx
```

If your installed SDK does not support the `.slnx` format, open the individual `.csproj` files or use a compatible Visual Studio version.

### 4. Configure local settings

Configuration files and user authentication data are intentionally excluded from version control.

Before running the application, create the required local configuration and authentication data using the format expected by the source code. Do not copy real passwords, credentials, or personal data into the repository.

### 5. Run and test

Run the backend service and UserAgent according to their project configuration. Depending on the implementation, the backend may require administrator privileges or Windows service installation.

Start with a virtual printer for safe testing, then test with a physical printer. Verify that jobs are intercepted, authentication is requested, and authorized or rejected jobs behave as expected.

## Security Notes

- Never commit passwords, PINs, API keys, tokens, or real user records.
- Keep local configuration files and authentication data out of Git.
- Use test accounts and non-sensitive documents during development.
- Restrict access to the private repository to trusted collaborators.
- Review `.gitignore` before every upload.

## Current Limitations

Print interception behavior can differ across printer drivers and hardware. A successful test with a virtual printer does not guarantee that every physical printer will behave identically.

Service installation, authentication-data initialization, and printer-specific configuration may require additional setup on each computer.

## Contributing

1. Clone the repository after receiving access.
2. Create a branch for your changes.
3. Test your changes locally.
4. Commit and push your branch.
5. Open a pull request for review.

## Project Status

This is a college mini-project under development. Features, compatibility, and setup instructions may evolve as implementation and testing continue.