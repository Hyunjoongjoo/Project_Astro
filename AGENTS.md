# AGENTS.md

## Project
- Unity 6 multiplayer team project
- Language: C#
- Networking: Photon Fusion

## Goal
- Preserve the current project structure
- Focus analysis on gameplay, placement, and stat application systems

## Rules
- Prefer minimal and safe edits
- Do not rename serialized fields unless absolutely necessary
- Do not change network authority flow without explaining the reason first
- After any change, explain which scripts were modified and why
- Preserve existing public APIs unless there is a clear reason to change them

## Naming Conventions
- Private fields must use underscore prefix
- Use camelCase for private fields
- Use PascalCase for public properties and methods
- Serialized fields should remain private and use [SerializeField] when needed

## Do Not Modify
- Scene or prefab references unless required
- Core multiplayer flow without a clear reason
- Inspector-linked fields unless necessary

## Validation
- Point out any possible Unity Inspector relinking issues
- Mention side effects that may affect multiplayer behavior

## Working Style
- Analyze first before making code changes
- Ask for broad refactoring only when necessary
- When discussing systems, distinguish clearly between likely user-owned code and team-owned/shared code