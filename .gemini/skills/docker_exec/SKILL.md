---
name: docker_exec
description: Execute commands in the persistent EstheticsByElizabeth API container
---

# Docker Exec Skill

This skill allows you to run commands inside the persistent `esthetics-api` Docker container. This is useful for running tests, database migrations, or other maintenance tasks without restarting the container.

## Usage

To use this skill, run the `scripts/dexec` script from the project root.

### Syntax

```bash
./scripts/dexec [command]
```

### Examples

- **Run unit tests:**
  ```bash
  ./scripts/dexec dotnet test
  ```

- **List files in the app directory:**
  ```bash
  ./scripts/dexec ls -la
  ```

- **Check environment variables:**
  ```bash
  ./scripts/dexec env
  ```

## Notes

- The script automatically starts the container if it's not running.
- If no command is provided, it opens an interactive shell (for human users).
- The working directory inside the container is `/app`.
