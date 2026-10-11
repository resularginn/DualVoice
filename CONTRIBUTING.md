# Contributing to DualVoice

Thank you for your interest in contributing to DualVoice! We welcome all contributions that help improve performance, add features, or polish the user experience.

## How to Contribute

1. **Fork the Repository:** Create your own fork of the project.
2. **Create a Feature Branch:** `git checkout -b feature/amazing-feature`
3. **Make Your Changes:** Test and ensure `DualVoice.exe` builds cleanly without warnings or errors.
4. **Test Thoroughly:** Verify Bluetooth audio mirroring with two playback devices on Windows 10/11.
5. **Commit Your Changes:** `git commit -m 'Add amazing feature'`
6. **Push to Your Branch:** `git push origin feature/amazing-feature`
7. **Open a Pull Request:** Submit a clear, detailed PR description.

## Building Locally

No external SDKs or runtime installations are required. Simply run:

```cmd
build.bat
```

This uses the built-in Microsoft .NET Framework C# compiler (`csc.exe`) available on all modern Windows installations.

## Guidelines

- Keep the application lightweight, standalone, and free of third-party runtime dependencies.
- Maintain high-contrast, tactile UI aesthetics.
- Preserve thread safety and background audio stability.
