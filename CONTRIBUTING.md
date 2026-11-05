# Contributing

Start with the [roadmap](ROADMAP.md) and [implementation plan](docs/Core-Implementation-Plan.md). This repository contains wrapper/acquisition infrastructure and the first five OSRoot metadata observations. Model/file/process observations are next; other domains remain binding placeholders. Propose focused changes and preserve existing wrapper contracts and licensing notices.

Use Visual Studio 2022 17.14 with the .NET 9.0.3xx SDK, or the matching CLI. Run dotnet build StaadPro.Interop.Core.sln -c Release and dotnet test StaadPro.Interop.Core.sln -c Release. Offline tests require no STAAD installation. Live tests must be explicit and use controlled models.

Keep domain signatures in interfaces and code in one non-partial adapter. Reuse named types/enums, use descriptive parameters, document differences from similar APIs and provide meaningful tests. Keep native by-reference buffers internal and avoid tuple-based public contracts. Do not copy unreviewed asynchronous COM calls from other projects.

Use [GitHub Issues](https://github.com/StruXllence/StaadPro.Interop.Core/issues) for reproducible defects and proposed improvements. Never include credentials or confidential models in issues.
