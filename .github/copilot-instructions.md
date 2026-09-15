# Copilot instructions

- This repository is intentionally a classic, non-SDK .NET Framework 4.8 solution.
- Preserve WCF `BasicHttpBinding`, explicit `DataContract` DTOs, typed faults, EF6, and `packages.config` unless a task explicitly changes the architecture.
- Keep transport, business, and persistence concerns in their existing projects.
- Maintain `externalId` as the transaction idempotency key and retain the database unique index.
- Never add credentials, plaintext secrets, insecure deserialization, detailed production faults, or internet-facing HTTP guidance.
- Use deterministic, synthetic `.example.test` data only.
- Build with Visual Studio MSBuild and run both unit and LocalDB/WCF integration tests before submitting changes.
