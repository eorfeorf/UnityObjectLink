# URI specification

日本語版: [UriSpecification-ja.md](UriSpecification-ja.md)

## Version 1

```text
<scheme>://select?v=1&project=<project-id>&object=<percent-encoded-global-object-id>
```

The generated order is `v`, `project`, `object`. Parsers accept any order but require exactly one occurrence of every parameter and reject all unknown parameters.

## Fields

- `scheme`: RFC 3986 scheme syntax, restricted to 1–32 ASCII characters. It is normalized to lowercase.
- action/host: exactly `select`, compared case-insensitively.
- `v`: decimal `1` only.
- `project`: 1–64 ASCII letters, digits, `.`, `_`, or `-`; it must start with a letter or digit and cannot contain `..`.
- `object`: a percent-encoded Unity `GlobalObjectId_V1-...` string, decoded length at most 512 characters.

The complete URI is limited to 8192 UTF-16 characters in Unity and 8192 shell string characters in each handler. Control characters, malformed percent escapes, a path other than an optional `/`, user information, a port, and fragments are rejected.

## Matching

Unity compares the URI scheme to the current setting without case sensitivity and compares Project ID exactly. A mismatch never falls back to another running project. Version 1 exposes only selection; it cannot carry a command, path, method name, or arbitrary payload.

## Encoding example

```text
unity-object-link://select?v=1&project=sample-project&object=GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-123-0
```

The API always uses `Uri.EscapeDataString` for parameter values, even when the current `GlobalObjectId` happens to contain only URI-safe characters.
