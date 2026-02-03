# AGENTS.md

## Review guidelines
- Do not add or commit Unity-generated folders: Library/, Temp/, Obj/, Logs/, Builds/.
- Keep changes minimal and focused per PR.
- Put all new scripts under Assets/Scripts/.
- Do not change ProjectSettings/ unless explicitly required by the task.
- Do not add new Unity packages unless explicitly requested.
- Preserve .meta files; never delete or regenerate assets without reason.

## Working agreements
- Prefer simple, readable C# over clever abstractions.
- Do not change gameplay rules, if you want update docs/mvp.md tell me.
- Ensure the project runs in the Editor after changes (no compile errors).
