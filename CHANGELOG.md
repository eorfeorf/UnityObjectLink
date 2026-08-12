# Changelog

日本語版: [CHANGELOG-ja.md](CHANGELOG-ja.md)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Editor-only UPM package for versioned links to Unity objects.
- Project Settings for URI scheme, stable Project ID, receiver health, and protocol registration.
- Public link creation, parsing, handling result, and notification APIs.
- Asset, GameObject, and Tools menu commands for the active selection.
- Heartbeat and atomic per-project inbox transport with TTL, size, duplicate, and corruption checks.
- Windows per-user protocol registration and dispatch script.
- macOS AppleScript application generation, Launch Services registration, and dispatch script.
- EditMode coverage for URI validation, storage, inbox processing, assets, sub-assets, Prefabs, and Scene objects.
- Self-cleaning Windows/macOS protocol-handler E2E scripts and portable macOS dispatch and installer-logic tests.
- A full Windows OS-activation-to-Unity-selection EditMode E2E test, registration ownership protection, and visible inbox receive state.
- Safe generated Project IDs, pending-scheme cleanup enforcement, and rejection of Scene objects with unsaved changes.
- Bilingual English/Japanese architecture, URI, public API, security, platform, and client compatibility documentation.
